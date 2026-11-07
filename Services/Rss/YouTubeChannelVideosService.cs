using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws.Services.Rss;

/// <summary>
/// Citește toate videoclipurile unui canal YouTube din pagina publică „Videoclipuri” și din paginile următoare.
/// Feedul RSS al canalului conține doar ultimele videoclipuri; această cale nu folosește o cheie API.
/// </summary>
internal static class YouTubeChannelVideosService
{
    private const string WebClientVersion = "2.20251020.01.00";
    private const string ConsentCookie = "CONSENT=YES+1; SOCS=CAI";
    private static readonly Regex ChannelIdPattern = new(@"^UC[\w-]{22}$", RegexOptions.CultureInvariant);
    private static readonly HttpClient Client = YouTubeCommon.CreateSearchClient();

    /// <summary>Identificatorul canalului dintr-un feed YouTube de forma feeds/videos.xml?channel_id=….</summary>
    internal static bool TryGetChannelId(string? feedUrl, out string channelId)
    {
        channelId = string.Empty;
        if (!Uri.TryCreate(feedUrl, UriKind.Absolute, out var uri)) return false;
        if (!uri.Host.EndsWith("youtube.com", StringComparison.OrdinalIgnoreCase) || uri.AbsolutePath != "/feeds/videos.xml") return false;

        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            if (separator < 0 || pair[..separator] != "channel_id") continue;
            var candidate = Uri.UnescapeDataString(pair[(separator + 1)..]);
            if (!ChannelIdPattern.IsMatch(candidate)) return false;
            channelId = candidate;
            return true;
        }
        return false;
    }

    /// <summary>Prima pagină cu videoclipurile canalului.</summary>
    internal static Task<YouTubeVideoSearchService.YouTubeVideoPage> FirstPageAsync(string channelId, CancellationToken cancellationToken = default) =>
        FirstPageAsync(channelId, Client, cancellationToken);

    internal static async Task<YouTubeVideoSearchService.YouTubeVideoPage> FirstPageAsync(
        string channelId,
        HttpClient client,
        CancellationToken cancellationToken = default)
    {
        if (!ChannelIdPattern.IsMatch(channelId ?? string.Empty))
            throw new InvalidOperationException(UiText.Translate("Feedul selectat nu este un canal YouTube."));

        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://www.youtube.com/channel/{Uri.EscapeDataString(channelId)}/videos?hl=en");
        request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml");
        // Fără cookie-ul de consimțământ, YouTube redirecționează din UE la pagina de consimțământ, fără date.
        request.Headers.TryAddWithoutValidation("Cookie", ConsentCookie);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        EnsureSuccess(response);

        var html = await YouTubeCommon.ReadPageAsync(response.Content, cancellationToken).ConfigureAwait(false);
        if (!YouTubeCommon.TryFindInitialData(html, out var json))
            throw new InvalidOperationException(UiText.Translate("YouTube nu a returnat pagina videoclipurilor canalului. Încearcă din nou mai târziu."));
        return YouTubeVideoSearchService.ParseContinuationJson(json);
    }

    /// <summary>Pagina următoare, cu jetonul primit de la pagina anterioară.</summary>
    internal static Task<YouTubeVideoSearchService.YouTubeVideoPage> NextPageAsync(string continuationToken, CancellationToken cancellationToken = default) =>
        NextPageAsync(continuationToken, Client, cancellationToken);

    internal static async Task<YouTubeVideoSearchService.YouTubeVideoPage> NextPageAsync(
        string continuationToken,
        HttpClient client,
        CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.Serialize(new
        {
            context = new { client = new { clientName = "WEB", clientVersion = WebClientVersion, hl = "en", gl = "US" } },
            continuation = continuationToken
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://www.youtube.com/youtubei/v1/browse?prettyPrint=false")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("X-YouTube-Client-Name", "1");
        request.Headers.TryAddWithoutValidation("X-YouTube-Client-Version", WebClientVersion);
        request.Headers.TryAddWithoutValidation("Origin", "https://www.youtube.com");
        request.Headers.TryAddWithoutValidation("Cookie", ConsentCookie);
        request.Headers.Accept.ParseAdd("application/json");

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        EnsureSuccess(response);
        return YouTubeVideoSearchService.ParseContinuationJson(await YouTubeCommon.ReadPageAsync(response.Content, cancellationToken).ConfigureAwait(false));
    }

    private static void EnsureSuccess(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
            throw new InvalidOperationException(UiText.Translate("YouTube a limitat temporar căutarea video. Încearcă din nou mai târziu."));
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(UiText.Format("YouTube a răspuns cu eroarea {0}.", (int)response.StatusCode));
    }
}
