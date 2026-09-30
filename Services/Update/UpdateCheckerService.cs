using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws.Services.Update;

public static class UpdateCheckerService
{
    private const string ApiUrl = "https://api.github.com/repos/grifnas/OrizontRSS/releases/latest";
    private const long MaximumInstallerBytes = 512L * 1024 * 1024;
    private static readonly HttpClient HttpClient = new();
    private static string T(string source) => UiText.Translate(source);
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);

    public static Task<AppUpdateInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default) =>
        CheckForUpdatesAsync(HttpClient, ApiUrl, AppVersionInfo.DisplayVersion, cancellationToken);

    internal static async Task<AppUpdateInfo> CheckForUpdatesAsync(
        HttpClient client,
        string apiUrl,
        string currentVersion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.UserAgent.ParseAdd($"OrizontRSS/{currentVersion}");
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Failed(currentVersion, F("GitHub a răspuns cu codul HTTP {0}.", (int)response.StatusCode));

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return Failed(currentVersion, T("GitHub a returnat un răspuns de versiune nevalid."));

            var tag = ReadString(root, "tag_name");
            if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(currentVersion))
                return Failed(currentVersion, T("Eticheta versiunii curente sau publicate lipsește."));
            var latest = CleanVersion(tag);
            var current = CleanVersion(currentVersion);
            if (!Version.TryParse(latest, out var latestParsed) || !Version.TryParse(current, out var currentParsed))
                return Failed(currentVersion, T("Numărul versiunii curente sau publicate nu este valid."));

            var releaseUrl = ReadString(root, "html_url");
            if (!IsGitHubProjectUrl(releaseUrl))
                return Failed(currentVersion, T("Adresa paginii release-ului GitHub nu este validă."));

            if (latestParsed <= currentParsed)
            {
                return new AppUpdateInfo
                {
                    Status = UpdateCheckStatus.UpToDate,
                    LatestVersion = latest,
                    CurrentVersion = currentVersion,
                    ReleaseNotes = ReadString(root, "body"),
                    ReleasePageUrl = releaseUrl
                };
            }

            var installer = FindVerifiedInstallerAssets(root, latest);
            if (installer is null)
                return Failed(currentVersion, T("Release-ul nu conține un instalator și un fișier SHA-256 corespunzător."));

            return new AppUpdateInfo
            {
                Status = UpdateCheckStatus.UpdateAvailable,
                LatestVersion = latest,
                CurrentVersion = currentVersion,
                ReleaseNotes = ReadString(root, "body"),
                ReleasePageUrl = releaseUrl,
                DownloadUrl = installer.Value.InstallerUrl,
                ChecksumDownloadUrl = installer.Value.ChecksumUrl
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Update check failed: {exception}");
            var message = exception switch
            {
                HttpRequestException => T("Conexiunea la GitHub nu a putut fi stabilită."),
                OperationCanceledException => T("Verificarea actualizărilor a expirat."),
                JsonException => T("GitHub a returnat un răspuns de versiune nevalid."),
                _ => T("Verificarea actualizărilor a eșuat din cauza unei erori neașteptate.")
            };
            return Failed(currentVersion, message);
        }
    }

    public static Task<VerifiedUpdateInstaller> DownloadAndVerifyAsync(
        AppUpdateInfo update,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default) =>
        DownloadAndVerifyAsync(update, HttpClient, Path.GetTempPath(), progress, cancellationToken);

    internal static async Task<VerifiedUpdateInstaller> DownloadAndVerifyAsync(
        AppUpdateInfo update,
        HttpClient client,
        string temporaryRoot,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (update.Status != UpdateCheckStatus.UpdateAvailable)
            throw new InvalidOperationException(T("Nu există o actualizare verificată disponibilă pentru descărcare."));
        if (!IsGitHubProjectUrl(update.DownloadUrl) || !IsGitHubProjectUrl(update.ChecksumDownloadUrl))
            throw new InvalidDataException(T("Adresa instalatorului sau a fișierului SHA-256 nu este un asset HTTPS al unui release GitHub."));

        var expectedHash = await DownloadExpectedHashAsync(client, update.ChecksumDownloadUrl, cancellationToken);
        var directory = Path.Combine(temporaryRoot, $"OrizontRSS-Update-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var installerPath = Path.Combine(directory, "OrizontSetup.exe");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, update.DownloadUrl);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(F("GitHub a răspuns cu codul HTTP {0}.", (int)response.StatusCode), null, response.StatusCode);

            var expectedLength = response.Content.Headers.ContentLength ?? -1L;
            if (expectedLength == 0 || expectedLength > MaximumInstallerBytes)
                throw new InvalidDataException(T("Dimensiunea instalatorului este nulă sau depășește limita de siguranță de 512 MiB."));

            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var destination = new FileStream(installerPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                var buffer = new byte[81920];
                long totalBytes = 0;
                while (true)
                {
                    var read = await source.ReadAsync(buffer.AsMemory(), cancellationToken);
                    if (read == 0) break;
                    totalBytes += read;
                    if (totalBytes > MaximumInstallerBytes)
                        throw new InvalidDataException(T("Instalatorul depășește limita de siguranță de 512 MiB."));
                    hash.AppendData(buffer, 0, read);
                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    if (expectedLength > 0 && progress is not null)
                        progress.Report(Math.Clamp((double)totalBytes / expectedLength * 100.0, 0, 100));
                }
                await destination.FlushAsync(cancellationToken);
                destination.Flush(flushToDisk: true);
                if (totalBytes == 0 || (expectedLength >= 0 && totalBytes != expectedLength))
                    throw new InvalidDataException(T("Descărcarea instalatorului este incompletă."));
            }

            var actualHash = hash.GetHashAndReset();
            var expectedHashBytes = Convert.FromHexString(expectedHash);
            if (!CryptographicOperations.FixedTimeEquals(actualHash, expectedHashBytes))
                throw new InvalidDataException(T("Suma SHA-256 a instalatorului nu corespunde sumei publicate."));

            if (!await HasWindowsExecutableHeaderAsync(installerPath, cancellationToken))
                throw new InvalidDataException(T("Fișierul descărcat nu este un executabil Windows valid."));

            return new VerifiedUpdateInstaller(directory, installerPath);
        }
        catch
        {
            TryDeleteDirectory(directory);
            throw;
        }
    }

    public static string CleanVersion(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "0.0.0";
        var match = Regex.Match(raw, @"\d+(?:\.\d+)+");
        return match.Success ? match.Value : raw.TrimStart('v', 'V');
    }

    public static bool IsNewerVersion(string remote, string current)
    {
        if (Version.TryParse(remote, out var remoteVersion) && Version.TryParse(current, out var currentVersion))
            return remoteVersion > currentVersion;
        return string.Compare(remote, current, StringComparison.OrdinalIgnoreCase) > 0;
    }

    internal static bool IsGitHubProjectUrl(string? address) =>
        Uri.TryCreate(address, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps &&
        uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) &&
        uri.AbsolutePath.StartsWith("/grifnas/OrizontRSS/releases/", StringComparison.OrdinalIgnoreCase);

    private static AppUpdateInfo Failed(string currentVersion, string details) => new()
    {
        Status = UpdateCheckStatus.Error,
        CurrentVersion = currentVersion,
        ErrorMessage = string.IsNullOrWhiteSpace(details) ? T("Eroare necunoscută la verificarea actualizărilor.") : details
    };

    private static string ReadString(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static (string InstallerUrl, string ChecksumUrl)? FindVerifiedInstallerAssets(JsonElement release, string version)
    {
        if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            return null;

        var urls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var asset in assets.EnumerateArray())
        {
            var name = ReadString(asset, "name");
            var url = ReadString(asset, "browser_download_url");
            if (!string.IsNullOrWhiteSpace(name) && IsGitHubProjectUrl(url)) urls.TryAdd(name, url);
        }

        foreach (var installerName in new[] { $"OrizontSetup-{version}.exe", "OrizontSetup.exe" })
        {
            if (urls.TryGetValue(installerName, out var installerUrl) &&
                urls.TryGetValue(installerName + ".sha256", out var checksumUrl))
                return (installerUrl, checksumUrl);
        }
        return null;
    }

    private static async Task<string> DownloadExpectedHashAsync(HttpClient client, string checksumUrl, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, checksumUrl);
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(F("GitHub a răspuns cu codul HTTP {0}.", (int)response.StatusCode), null, response.StatusCode);
        if (response.Content.Headers.ContentLength is > 8192)
            throw new InvalidDataException(T("Fișierul SHA-256 este prea mare."));
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var content = new MemoryStream();
        var buffer = new byte[1024];
        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0) break;
            if (content.Length + read > 8192)
                throw new InvalidDataException(T("Fișierul SHA-256 este prea mare."));
            content.Write(buffer, 0, read);
        }
        var text = Encoding.UTF8.GetString(content.GetBuffer(), 0, checked((int)content.Length));
        var match = Regex.Match(text, @"(?i)(?<![0-9a-f])[0-9a-f]{64}(?![0-9a-f])");
        if (!match.Success) throw new InvalidDataException(T("Fișierul SHA-256 publicat nu este valid."));
        return match.Value.ToUpperInvariant();
    }

    private static async Task<bool> HasWindowsExecutableHeaderAsync(string path, CancellationToken cancellationToken)
    {
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (file.Length < 68) return false;
        var dosHeader = new byte[64];
        await file.ReadExactlyAsync(dosHeader, cancellationToken);
        if (dosHeader[0] != (byte)'M' || dosHeader[1] != (byte)'Z') return false;
        var peOffset = BinaryPrimitives.ReadInt32LittleEndian(dosHeader.AsSpan(0x3c, 4));
        if (peOffset < dosHeader.Length || peOffset > file.Length - 4) return false;
        file.Position = peOffset;
        var signature = new byte[4];
        await file.ReadExactlyAsync(signature, cancellationToken);
        return signature[0] == (byte)'P' && signature[1] == (byte)'E' && signature[2] == 0 && signature[3] == 0;
    }

    private static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch { /* Temporary cleanup must not hide the original download/verification error. */ }
    }
}

public sealed class VerifiedUpdateInstaller : IDisposable
{
    private readonly string _temporaryDirectory;
    private bool _disposed;

    internal VerifiedUpdateInstaller(string temporaryDirectory, string installerPath)
    {
        _temporaryDirectory = temporaryDirectory;
        InstallerPath = installerPath;
    }

    public string InstallerPath { get; }

    internal Process Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Process.Start(new ProcessStartInfo(InstallerPath) { UseShellExecute = true })
            ?? throw new InvalidOperationException(UiText.Translate("Windows nu a pornit instalatorul verificat."));
    }

    internal void DeleteWhenProcessExits(Process process)
    {
        _ = Task.Run(async () =>
        {
            try { await process.WaitForExitAsync(); }
            catch { /* The temporary file is still best-effort cleanup. */ }
            finally
            {
                process.Dispose();
                Dispose();
            }
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { if (Directory.Exists(_temporaryDirectory)) Directory.Delete(_temporaryDirectory, recursive: true); }
        catch { /* Cleanup is best effort; never mask a successful save or update launch. */ }
    }
}
