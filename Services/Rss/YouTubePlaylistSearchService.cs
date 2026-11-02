using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using CititorRSS.Jaws.Localization;
using CititorRSS.Jaws.Models;
using static CititorRSS.Jaws.Services.Rss.YouTubeCommon;

namespace CititorRSS.Jaws.Services.Rss;

/// <summary>
/// Caută playlisturi prin pagina publică YouTube (fără cheie API), apoi verifică
/// fluxul RSS standard al fiecărui playlist. Structura paginii nu este un API stabil.
/// </summary>
public static class YouTubePlaylistSearchService
{
    private const string SearchEndpoint = "https://www.youtube.com/results";
    // Filtrul public pentru playlisturi; parametru YouTube neoficial, nu un endpoint API stabil.
    private const string PlaylistFilter = "EgIQAw%3D%3D";
    private const int MaximumPlaylistsToCheck = 20;
    private static readonly Regex PlaylistIdPattern = new(@"^[A-Za-z0-9_-]{10,80}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
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
            throw new InvalidOperationException(UiText.Translate("Tastează un cuvânt-cheie pentru căutarea playlisturilor YouTube."));

        var safeLanguage = string.IsNullOrWhiteSpace(language) ? "en" : language.Trim();
        var requestUri = $"{SearchEndpoint}?search_query={Uri.EscapeDataString(query)}&sp={PlaylistFilter}&hl={Uri.EscapeDataString(safeLanguage)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml");
        request.Headers.AcceptLanguage.ParseAdd($"{safeLanguage},en;q=0.8");

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
            throw new InvalidOperationException(UiText.Translate("YouTube a limitat temporar căutarea. Încearcă din nou mai târziu."));
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(UiText.Format("YouTube a răspuns cu eroarea {0}.", (int)response.StatusCode));

        var html = await ReadPageAsync(response.Content, cancellationToken).ConfigureAwait(false);
        var playlists = ParsePlaylists(html);
        if (playlists.Count == 0) return [];

        using var gate = new SemaphoreSlim(FeedDiscoveryService.MaximumParallelChecks);
        var verified = await Task.WhenAll(playlists.Select(async playlist =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            var feedUrl = BuildFeedUrl(playlist.PlaylistId);
            var webPageUrl = BuildPlaylistUrl(playlist.PlaylistId);
            try
            {
                var preview = await reader.LoadPreviewAsync(feedUrl, cancellationToken).ConfigureAwait(false);

                return new DiscoveredFeed
                {
                    Name = playlist.Name,
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
                // Păstrăm playlistul găsit; utilizatorul poate deschide pagina sau poate reîncerca verificarea RSS.
                return new DiscoveredFeed
                {
                    Name = playlist.Name,
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

    internal static List<YouTubePlaylistCandidate> ParsePlaylists(string html)
    {
        if (!TryFindInitialData(html, out var json))
            throw new InvalidOperationException(UiText.Translate("YouTube nu a returnat o pagină de căutare recunoscută. Încearcă din nou."));

        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 64 });
        var found = new List<YouTubePlaylistCandidate>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        CollectPlaylistRenderers(document.RootElement, found, seen);
        return found;
    }

    internal static string BuildFeedUrl(string playlistId) =>
        $"https://www.youtube.com/feeds/videos.xml?playlist_id={Uri.EscapeDataString(playlistId)}";

    internal static string BuildPlaylistUrl(string playlistId) =>
        $"https://www.youtube.com/playlist?list={Uri.EscapeDataString(playlistId)}";

    private static void CollectPlaylistRenderers(JsonElement element, List<YouTubePlaylistCandidate> found, HashSet<string> seen)
    {
        if (found.Count >= MaximumPlaylistsToCheck) return;

        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("lockupViewModel", out var lockup)
                && GetString(lockup, "contentType") == "LOCKUP_CONTENT_TYPE_PLAYLIST")
            {
                AddCandidate(GetString(lockup, "contentId"), ReadLockupTitle(lockup), found, seen);
            }

            if (element.TryGetProperty("playlistRenderer", out var legacy))
                AddCandidate(GetString(legacy, "playlistId"), ReadText(legacy, "title"), found, seen);

            foreach (var property in element.EnumerateObject())
            {
                if (found.Count >= MaximumPlaylistsToCheck) break;
                CollectPlaylistRenderers(property.Value, found, seen);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (found.Count >= MaximumPlaylistsToCheck) break;
                CollectPlaylistRenderers(item, found, seen);
            }
        }
    }

    private static void AddCandidate(string playlistId, string name, List<YouTubePlaylistCandidate> found, HashSet<string> seen)
    {
        if (PlaylistIdPattern.IsMatch(playlistId) && seen.Add(playlistId))
            found.Add(new YouTubePlaylistCandidate(string.IsNullOrWhiteSpace(name) ? playlistId : name, playlistId));
    }

    internal sealed record YouTubePlaylistCandidate(string Name, string PlaylistId);
}
