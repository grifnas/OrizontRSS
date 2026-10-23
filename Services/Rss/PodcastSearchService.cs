using System.Net.Http;
using System.Text.Json;
using CititorRSS.Jaws.Models;

namespace CititorRSS.Jaws.Services.Rss;

/// <summary>
/// Caută podcasturi în catalogul public Apple Podcasts (iTunes Search API). Nu cere cont sau cheie API.
/// Fiecare rezultat este adresa feed-ului RSS al podcastului, deci se adaugă ca orice alt feed.
/// </summary>
public static class PodcastSearchService
{
    private const string Endpoint = "https://itunes.apple.com/search";
    private const int MaximumResults = 25;
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(20) };

    public static async Task<List<DiscoveredFeed>> SearchAsync(string term, CancellationToken cancellationToken = default)
    {
        var query = $"{Endpoint}?media=podcast&entity=podcast&limit={MaximumResults}&term={Uri.EscapeDataString(term.Trim())}";
        using var response = await Client.GetAsync(query, cancellationToken);
        response.EnsureSuccessStatusCode();
        return Parse(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    /// <summary>Transformă răspunsul iTunes în feeduri; intrările fără adresă web validă sunt ignorate.</summary>
    internal static List<DiscoveredFeed> Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
            return [];

        var found = new List<DiscoveredFeed>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in results.EnumerateArray())
        {
            var feedUrl = Text(item, "feedUrl");
            if (string.IsNullOrWhiteSpace(feedUrl) || !Uri.TryCreate(feedUrl, UriKind.Absolute, out var uri)) continue;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) continue;
            if (!seen.Add(uri.AbsoluteUri)) continue;
            var name = Text(item, "collectionName");
            if (string.IsNullOrWhiteSpace(name)) name = uri.Host;
            found.Add(new DiscoveredFeed
            {
                Name = name,
                Url = uri.AbsoluteUri,
                SourceSite = "Apple Podcasts",
                RequiresVerification = true
            });
        }
        return found;
    }

    private static string Text(JsonElement item, string property) =>
        item.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;
}
