using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws.Services.Rss;

/// <summary>
/// Elemente comune serviciilor YouTube: client HTTP, citire limitată a paginilor, găsirea datelor
/// din pagina de căutare și citirea câmpurilor din JSON. Mesajele vizibile rămân în serviciile care le folosesc.
/// </summary>
internal static class YouTubeCommon
{
    internal const string InitialDataMarker = "var ytInitialData = ";
    internal const string InitialDataEnd = ";</script>";
    internal const int MaximumResponseBytes = 5 * 1024 * 1024;

    /// <summary>
    /// Client pentru paginile publice de căutare YouTube (timeout de 25 de secunde).
    /// </summary>
    internal static HttpClient CreateSearchClient()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = true,
            CheckCertificateRevocationList = true
        };
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(25) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36");
        return client;
    }

    /// <summary>
    /// Găsește blocul JSON „ytInitialData” dintr-o pagină YouTube. Întoarce false dacă blocul lipsește sau este gol.
    /// </summary>
    internal static bool TryFindInitialData(string html, out string json)
    {
        json = string.Empty;
        var dataStart = html.IndexOf(InitialDataMarker, StringComparison.Ordinal);
        if (dataStart < 0) return false;
        dataStart += InitialDataMarker.Length;

        var dataEnd = html.IndexOf(InitialDataEnd, dataStart, StringComparison.Ordinal);
        if (dataEnd <= dataStart) return false;

        json = html[dataStart..dataEnd];
        return true;
    }

    internal static string ReadLockupTitle(JsonElement lockup)
    {
        if (lockup.TryGetProperty("metadata", out var metadata)
            && metadata.TryGetProperty("lockupMetadataViewModel", out var viewModel)
            && viewModel.TryGetProperty("title", out var title))
        {
            var content = GetString(title, "content");
            if (!string.IsNullOrWhiteSpace(content)) return content.Trim();
            return ReadText(viewModel, "title");
        }
        return string.Empty;
    }

    internal static string ReadText(JsonElement renderer, string propertyName)
    {
        if (!renderer.TryGetProperty(propertyName, out var value)) return string.Empty;
        if (value.ValueKind == JsonValueKind.String) return value.GetString() ?? string.Empty;
        if (value.ValueKind != JsonValueKind.Object) return string.Empty;
        if (value.TryGetProperty("simpleText", out var simple) && simple.ValueKind == JsonValueKind.String)
            return simple.GetString() ?? string.Empty;
        if (!value.TryGetProperty("runs", out var runs) || runs.ValueKind != JsonValueKind.Array)
            return string.Empty;
        return string.Concat(runs.EnumerateArray().Select(run => GetString(run, "text"))).Trim();
    }

    internal static string GetString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(propertyName, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    /// <summary>
    /// Citește răspunsul în memorie, cu limita de <see cref="MaximumResponseBytes"/>.
    /// </summary>
    internal static async Task<string> ReadPageAsync(HttpContent content, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength is > MaximumResponseBytes)
            throw new InvalidOperationException(UiText.Translate("Rezultatul căutării YouTube este prea mare pentru a fi procesat în siguranță."));

        await using var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        while (true)
        {
            var read = await stream.ReadAsync(chunk.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            if (buffer.Length + read > MaximumResponseBytes)
                throw new InvalidOperationException(UiText.Translate("Rezultatul căutării YouTube este prea mare pentru a fi procesat în siguranță."));
            buffer.Write(chunk, 0, read);
        }
        return Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
    }
}
