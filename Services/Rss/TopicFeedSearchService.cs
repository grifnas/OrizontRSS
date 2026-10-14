using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using CititorRSS.Jaws.Models;

namespace CititorRSS.Jaws.Services.Rss;

public static class TopicFeedSearchService
{
    private static CatalogRoot? _cachedCatalog;

    public static CatalogRoot GetCatalog()
    {
        if (_cachedCatalog is not null) return _cachedCatalog;

        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "FeedCatalog.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                _cachedCatalog = JsonSerializer.Deserialize<CatalogRoot>(json);
            }
        }
        catch
        {
            // Fallback to empty catalog if JSON file cannot be loaded.
        }

        return _cachedCatalog ??= new CatalogRoot();
    }

    public static List<CatalogFeedItem> SearchCatalog(string? query, string? selectedCategory)
    {
        var catalog = GetCatalog();
        var allFeeds = catalog.Categories.SelectMany(c => c.Feeds).ToList();

        // null sau gol înseamnă toate categoriile; fereastra trimite cheia categoriei, nu textul tradus.
        if (!string.IsNullOrWhiteSpace(selectedCategory) &&
            !string.Equals(selectedCategory, "Toate categoriile", StringComparison.OrdinalIgnoreCase))
        {
            allFeeds = allFeeds.Where(f => string.Equals(f.Category, selectedCategory, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return allFeeds;
        }

        var normalizedQuery = ArticleSearch.Normalize(query.Trim());
        return allFeeds.Where(f =>
            ArticleSearch.Normalize(f.Name).Contains(normalizedQuery, StringComparison.Ordinal) ||
            ArticleSearch.Normalize(f.Description).Contains(normalizedQuery, StringComparison.Ordinal) ||
            ArticleSearch.Normalize(f.Category).Contains(normalizedQuery, StringComparison.Ordinal)
        ).ToList();
    }

    // Căutarea pe internet este în FeedDiscoveryService.SearchTopicAsync (Feedly, cu verificarea rezultatelor).
}
