using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using CititorRSS.Jaws.Localization;
using CititorRSS.Jaws.Models;
using static CititorRSS.Jaws.Services.Rss.YouTubeCommon;

namespace CititorRSS.Jaws.Services.Rss;

/// <summary>
/// Caută canale prin pagina publică de rezultate YouTube (fără cheie API), apoi verifică
/// fluxul RSS standard al fiecărui canal. Căutarea depinde de structura publică a paginii YouTube.
/// </summary>
public static class YouTubeChannelSearchService
{
    private const string SearchEndpoint = "https://www.youtube.com/results";
    // Filtrul paginii publice pentru tipul „canal”; este un parametru YouTube neoficial, nu un endpoint API stabil.
    private const string ChannelFilter = "EgIQAg%3D%3D";
    private const int MaximumChannelsToCheck = 20;
    private static readonly Regex ChannelIdPattern = new(@"^UC[\w-]{22}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly HttpClient Client = CreateSearchClient();
    private static readonly RssReader Reader = new();

    public static Task<List<DiscoveredFeed>> SearchAsync(string query, string language, CancellationToken cancellationToken = default) =>
        SearchAsync(query, language, Client, Reader, cancellationToken);

    internal static async Task<List<DiscoveredFeed>> SearchAsync(
        string query,
        string language,
        HttpClient client,
        RssReader reader,
        CancellationToken cancellationToken = default)
    {
        query = (query ?? string.Empty).Trim();
        if (query.Length == 0)
            throw new InvalidOperationException(UiText.Translate("Tastează un cuvânt-cheie pentru căutarea canalelor YouTube."));

        var safeLanguage = string.IsNullOrWhiteSpace(language) ? "en" : language.Trim();
        var requestUri = $"{SearchEndpoint}?search_query={Uri.EscapeDataString(query)}&sp={ChannelFilter}&hl={Uri.EscapeDataString(safeLanguage)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml");
        request.Headers.AcceptLanguage.ParseAdd($"{safeLanguage},en;q=0.8");

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
            throw new InvalidOperationException(UiText.Translate("YouTube a limitat temporar căutarea. Încearcă din nou mai târziu sau introdu adresa canalului."));
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(UiText.Format("YouTube a răspuns cu eroarea {0}.", (int)response.StatusCode));

        var html = await ReadPageAsync(response.Content, cancellationToken).ConfigureAwait(false);
        var channels = ParseChannels(html);
        if (channels.Count == 0) return [];

        using var gate = new SemaphoreSlim(FeedDiscoveryService.MaximumParallelChecks);
        var verified = await Task.WhenAll(channels.Select(async channel =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            var feedUrl = BuildFeedUrl(channel.ChannelId);
            var webPageUrl = BuildChannelUrl(channel.ChannelId);
            try
            {
                var preview = await reader.LoadPreviewAsync(feedUrl, cancellationToken).ConfigureAwait(false);

                return new DiscoveredFeed
                {
                    Name = channel.Name,
                    Url = preview.Url,
                    WebPageUrl = webPageUrl,
                    SourceSite = "YouTube",
                    IsVerified = true,
                    ArticleCount = preview.Articles.Count,
                    LatestArticle = preview.Articles.Max(article => article.Published)
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // Păstrăm canalul găsit; utilizatorul poate deschide pagina sau poate reîncerca verificarea RSS.
                return new DiscoveredFeed
                {
                    Name = channel.Name,
                    Url = feedUrl,
                    WebPageUrl = webPageUrl,
                    SourceSite = "YouTube",
                    IsVerified = false
                };
            }
            finally
            {
                gate.Release();
            }
        })).ConfigureAwait(false);

        return verified.ToList();
    }

    internal static List<YouTubeChannelCandidate> ParseChannels(string html)
    {
        if (!TryFindInitialData(html, out var json))
            throw new InvalidOperationException(UiText.Translate("YouTube nu a returnat o pagină de căutare recunoscută. Încearcă din nou sau introdu adresa canalului."));

        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 64 });
        var found = new List<YouTubeChannelCandidate>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        CollectChannelRenderers(document.RootElement, found, seen);
        return found;
    }

    internal static string BuildFeedUrl(string channelId) =>
        $"https://www.youtube.com/feeds/videos.xml?channel_id={Uri.EscapeDataString(channelId)}";

    internal static string BuildChannelUrl(string channelId) =>
        $"https://www.youtube.com/channel/{Uri.EscapeDataString(channelId)}";

    private static void CollectChannelRenderers(JsonElement element, List<YouTubeChannelCandidate> found, HashSet<string> seen)
    {
        if (found.Count >= MaximumChannelsToCheck) return;

        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("lockupViewModel", out var lockup)
                && GetString(lockup, "contentType") == "LOCKUP_CONTENT_TYPE_CHANNEL")
            {
                var id = GetString(lockup, "contentId");
                if (ChannelIdPattern.IsMatch(id) && seen.Add(id))
                {
                    var name = ReadLockupTitle(lockup);
                    found.Add(new YouTubeChannelCandidate(string.IsNullOrWhiteSpace(name) ? id : name, id));
                }
            }

            if (element.TryGetProperty("channelRenderer", out var renderer))
            {
                var id = GetString(renderer, "channelId");
                if (ChannelIdPattern.IsMatch(id) && seen.Add(id))
                {
                    var name = ReadText(renderer, "title");
                    found.Add(new YouTubeChannelCandidate(string.IsNullOrWhiteSpace(name) ? id : name, id));
                }
            }

            foreach (var property in element.EnumerateObject())
            {
                if (found.Count >= MaximumChannelsToCheck) break;
                CollectChannelRenderers(property.Value, found, seen);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (found.Count >= MaximumChannelsToCheck) break;
                CollectChannelRenderers(item, found, seen);
            }
        }
    }

    internal sealed record YouTubeChannelCandidate(string Name, string ChannelId);
}
