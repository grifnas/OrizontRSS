using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CititorRSS.Jaws.Localization;
using static CititorRSS.Jaws.Services.Rss.YouTubeCommon;

namespace CititorRSS.Jaws.Services.Rss;

/// <summary>
/// Caută videoclipuri prin pagina publică YouTube (fără cheie API). Este folosită
/// doar pentru descoperire; redarea se deschide în browserul implicit al utilizatorului.
/// </summary>
public static class YouTubeVideoSearchService
{
    private const string SearchEndpoint = "https://www.youtube.com/results";
    // Filtrul public pentru videoclipuri; parametru YouTube neoficial, nu un endpoint API stabil.
    private const string VideoFilter = "EgIQAQ%3D%3D";
    private const int MaximumVideosPerPage = 60;
    private const string WebClientVersion = "2.20251020.01.00";
    private static readonly Regex VideoIdPattern = new(@"^[A-Za-z0-9_-]{11}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ChannelIdPattern = new(@"^UC[\w-]{22}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly HttpClient Client = CreateSearchClient();

    internal static Task<List<YouTubeVideoCandidate>> SearchAsync(string query, string language, CancellationToken cancellationToken = default) =>
        SearchAsync(query, language, Client, cancellationToken);

    internal static async Task<List<YouTubeVideoCandidate>> SearchAsync(
        string query,
        string language,
        HttpClient client,
        CancellationToken cancellationToken = default) =>
        (await SearchPageAsync(query, language, string.Empty, client, cancellationToken).ConfigureAwait(false)).Videos;

    /// <summary>
    /// Prima pagină de rezultate (fără jeton) sau pagina următoare (cu jetonul de continuare primit anterior).
    /// </summary>
    internal static Task<YouTubeVideoPage> SearchPageAsync(
        string query,
        string language,
        string continuationToken,
        CancellationToken cancellationToken = default) =>
        SearchPageAsync(query, language, continuationToken, Client, cancellationToken);

    internal static async Task<YouTubeVideoPage> SearchPageAsync(
        string query,
        string language,
        string continuationToken,
        HttpClient client,
        CancellationToken cancellationToken = default)
    {
        query = (query ?? string.Empty).Trim();
        if (query.Length == 0)
            throw new InvalidOperationException(UiText.Translate("Tastează un cuvânt-cheie pentru căutarea videoclipurilor YouTube."));

        var safeLanguage = string.IsNullOrWhiteSpace(language) ? "en" : language.Trim();
        if (!string.IsNullOrWhiteSpace(continuationToken))
            return await SearchContinuationAsync(continuationToken, safeLanguage, client, cancellationToken).ConfigureAwait(false);

        var requestUri = $"{SearchEndpoint}?search_query={Uri.EscapeDataString(query)}&sp={VideoFilter}&hl={Uri.EscapeDataString(safeLanguage)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml");
        request.Headers.AcceptLanguage.ParseAdd($"{safeLanguage},en;q=0.8");

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
            throw new InvalidOperationException(UiText.Translate("YouTube a limitat temporar căutarea video. Încearcă din nou mai târziu."));
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(UiText.Format("YouTube a răspuns cu eroarea {0}.", (int)response.StatusCode));

        return ParsePage(await ReadPageAsync(response.Content, cancellationToken).ConfigureAwait(false));
    }

    private static async Task<YouTubeVideoPage> SearchContinuationAsync(
        string continuationToken,
        string language,
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(new
        {
            context = new { client = new { clientName = "WEB", clientVersion = WebClientVersion, hl = language, gl = "US" } },
            continuation = continuationToken
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://www.youtube.com/youtubei/v1/search?prettyPrint=false")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("X-YouTube-Client-Name", "1");
        request.Headers.TryAddWithoutValidation("X-YouTube-Client-Version", WebClientVersion);
        request.Headers.TryAddWithoutValidation("Origin", "https://www.youtube.com");
        request.Headers.Accept.ParseAdd("application/json");

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
            throw new InvalidOperationException(UiText.Translate("YouTube a limitat temporar căutarea video. Încearcă din nou mai târziu."));
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(UiText.Format("YouTube a răspuns cu eroarea {0}.", (int)response.StatusCode));

        return ParseContinuationJson(await ReadPageAsync(response.Content, cancellationToken).ConfigureAwait(false));
    }

    internal static YouTubeVideoPage ParseContinuationJson(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 64 });
        return BuildPage(document.RootElement);
    }

    internal static List<YouTubeVideoCandidate> ParseVideos(string html) => ParsePage(html).Videos;

    internal static YouTubeVideoPage ParsePage(string html)
    {
        if (!TryFindInitialData(html, out var json))
            throw new InvalidOperationException(UiText.Translate("YouTube nu a returnat o pagină de căutare video recunoscută. Încearcă din nou."));

        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 64 });
        return BuildPage(document.RootElement);
    }

    private static YouTubeVideoPage BuildPage(JsonElement root)
    {
        var found = new List<YouTubeVideoCandidate>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        CollectVideoRenderers(root, found, seen);
        return new YouTubeVideoPage(found, FindContinuationToken(root));
    }

    /// <summary>
    /// Jetonul paginii următoare este în continuationItemRenderer → continuationEndpoint → continuationCommand → token.
    /// </summary>
    private static string FindContinuationToken(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("continuationItemRenderer", out var renderer)
                && renderer.ValueKind == JsonValueKind.Object
                && renderer.TryGetProperty("continuationEndpoint", out var endpoint)
                && endpoint.ValueKind == JsonValueKind.Object
                && endpoint.TryGetProperty("continuationCommand", out var command)
                && command.ValueKind == JsonValueKind.Object)
            {
                var token = GetString(command, "token");
                if (token.Length > 0) return token;
            }
            foreach (var property in element.EnumerateObject())
            {
                var token = FindContinuationToken(property.Value);
                if (token.Length > 0) return token;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var token = FindContinuationToken(item);
                if (token.Length > 0) return token;
            }
        }
        return string.Empty;
    }

    private static void CollectVideoRenderers(JsonElement element, List<YouTubeVideoCandidate> found, HashSet<string> seen)
    {
        if (found.Count >= MaximumVideosPerPage) return;

        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("lockupViewModel", out var lockup)
                && GetString(lockup, "contentType") == "LOCKUP_CONTENT_TYPE_VIDEO")
            {
                AddCandidate(
                    GetString(lockup, "contentId"),
                    ReadLockupTitle(lockup),
                    ReadLockupChannel(lockup),
                    FindChannelId(lockup),
                    found,
                    seen);
            }

            if (element.TryGetProperty("videoRenderer", out var renderer))
            {
                AddCandidate(
                    GetString(renderer, "videoId"),
                    ReadText(renderer, "title"),
                    ReadText(renderer, "ownerText"),
                    FindChannelId(renderer),
                    found,
                    seen);
            }

            foreach (var property in element.EnumerateObject())
            {
                if (found.Count >= MaximumVideosPerPage) break;
                CollectVideoRenderers(property.Value, found, seen);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (found.Count >= MaximumVideosPerPage) break;
                CollectVideoRenderers(item, found, seen);
            }
        }
    }

    private static void AddCandidate(string videoId, string title, string channel, string channelId, List<YouTubeVideoCandidate> found, HashSet<string> seen)
    {
        if (VideoIdPattern.IsMatch(videoId) && seen.Add(videoId))
            found.Add(new YouTubeVideoCandidate(
                string.IsNullOrWhiteSpace(title) ? videoId : title,
                videoId,
                channel,
                channelId));
    }

    private static string FindChannelId(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("browseId", out var browseId)
                && browseId.ValueKind == JsonValueKind.String
                && ChannelIdPattern.IsMatch(browseId.GetString() ?? string.Empty))
            {
                return browseId.GetString()!;
            }
            foreach (var property in element.EnumerateObject())
            {
                var found = FindChannelId(property.Value);
                if (found.Length > 0) return found;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var found = FindChannelId(item);
                if (found.Length > 0) return found;
            }
        }
        return string.Empty;
    }

    private static string ReadLockupChannel(JsonElement lockup)
    {
        if (!lockup.TryGetProperty("metadata", out var metadata)
            || !metadata.TryGetProperty("lockupMetadataViewModel", out var viewModel)
            || !viewModel.TryGetProperty("metadata", out var container)
            || !container.TryGetProperty("contentMetadataViewModel", out var contentMetadata)
            || !contentMetadata.TryGetProperty("metadataRows", out var rows)
            || rows.ValueKind != JsonValueKind.Array)
            return string.Empty;

        foreach (var row in rows.EnumerateArray())
        {
            if (!row.TryGetProperty("metadataParts", out var parts) || parts.ValueKind != JsonValueKind.Array)
                continue;
            foreach (var part in parts.EnumerateArray())
            {
                if (!part.TryGetProperty("text", out var text)) continue;
                var value = GetString(text, "content");
                if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
            }
        }
        return string.Empty;
    }

    internal sealed record YouTubeVideoPage(List<YouTubeVideoCandidate> Videos, string ContinuationToken);

    public sealed record YouTubeVideoCandidate(string Title, string VideoId, string ChannelName, string ChannelId = "")
    {
        public string Url => $"https://www.youtube.com/watch?v={Uri.EscapeDataString(VideoId)}";
        public string Name => Title;
        public string Status => string.IsNullOrWhiteSpace(ChannelName)
            ? string.Empty
            : UiText.Format("Canal YouTube: {0}.", ChannelName);
        public string DisplayName => string.IsNullOrWhiteSpace(ChannelName)
            ? Title
            : UiText.Format("{0}. Canal YouTube: {1}.", Title, ChannelName);
    }
}
