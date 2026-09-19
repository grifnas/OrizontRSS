using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;

namespace CititorRSS.Jaws.Services.Update;

public static class UpdateCheckerService
{
    private const string ApiUrl = "https://api.github.com/repos/grifnas/OrizontRSS/releases/latest";
    private static readonly HttpClient HttpClient = new();

    public static async Task<AppUpdateInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var currentVersion = AppVersionInfo.DisplayVersion;
        var result = new AppUpdateInfo
        {
            CurrentVersion = currentVersion,
            HasUpdate = false
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl);
            request.Headers.UserAgent.ParseAdd($"OrizontRSS/{currentVersion}");

            using var response = await HttpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return result;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() ?? string.Empty : string.Empty;
            var body = root.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? string.Empty : string.Empty;
            var htmlUrl = root.TryGetProperty("html_url", out var htmlProp) ? htmlProp.GetString() ?? string.Empty : string.Empty;

            var cleanRemote = CleanVersion(tagName);
            var cleanCurrent = CleanVersion(currentVersion);

            result.LatestVersion = cleanRemote;
            result.ReleaseNotes = body;
            result.ReleasePageUrl = htmlUrl;

            var downloadUrl = string.Empty;
            if (root.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assetsProp.EnumerateArray())
                {
                    var name = asset.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? string.Empty : string.Empty;
                    if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || name.Contains("Setup", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.TryGetProperty("browser_download_url", out var dlProp) ? dlProp.GetString() ?? string.Empty : string.Empty;
                        break;
                    }
                }
            }

            result.DownloadUrl = string.IsNullOrEmpty(downloadUrl) ? htmlUrl : downloadUrl;

            if (IsNewerVersion(cleanRemote, cleanCurrent))
            {
                result.HasUpdate = true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update check failed: {ex.Message}");
        }

        return result;
    }

    public static async Task DownloadAndInstallAsync(string downloadUrl, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var tempFolder = Path.GetTempPath();
        var tempExe = Path.Combine(tempFolder, "OrizontSetup-Update.exe");

        using var response = await HttpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream = new FileStream(tempExe, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        var totalRead = 0L;
        int read;

        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            totalRead += read;
            if (totalBytes > 0 && progress is not null)
            {
                progress.Report((double)totalRead / totalBytes * 100.0);
            }
        }

        fileStream.Close();

        // Launch installer and shutdown current application
        Process.Start(new ProcessStartInfo(tempExe) { UseShellExecute = true });
        Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
    }

    public static string CleanVersion(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "0.0.0";
        var match = Regex.Match(raw, @"\d+(?:\.\d+)+");
        return match.Success ? match.Value : raw.TrimStart('v', 'V');
    }

    public static bool IsNewerVersion(string remote, string current)
    {
        if (Version.TryParse(remote, out var remoteVer) && Version.TryParse(current, out var currentVer))
        {
            return remoteVer > currentVer;
        }
        return string.Compare(remote, current, StringComparison.OrdinalIgnoreCase) > 0;
    }
}
