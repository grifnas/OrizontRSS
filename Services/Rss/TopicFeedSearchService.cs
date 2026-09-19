using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using CititorRSS.Jaws.Models;

namespace CititorRSS.Jaws.Services.Rss;

public static class TopicFeedSearchService
{
    private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
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

    public static async Task<List<CatalogFeedItem>> SearchLiveAsync(string query)
    {
        var results = new List<CatalogFeedItem>();
        if (string.IsNullOrWhiteSpace(query)) return results;

        try
        {
            // Query open Feedsearch API for live feeds matching the topic query
            var searchUrl = $"https://feedsearch.dev/api/v1/search?url={Uri.EscapeDataString(query.Trim())}";
            var response = await HttpClient.GetAsync(searchUrl);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in doc.RootElement.EnumerateArray())
                    {
                        var url = element.TryGetProperty("url", out var u) ? u.GetString() : null;
                        var title = element.TryGetProperty("title", out var t) ? t.GetString() : null;
                        var description = element.TryGetProperty("description", out var d) ? d.GetString() : null;

                        if (!string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(title))
                        {
                            results.Add(new CatalogFeedItem
                            {
                                Name = title.Trim(),
                                Url = url.Trim(),
                                Category = "Căutare live web",
                                Description = string.IsNullOrWhiteSpace(description) ? "Sursă găsită pe internet" : description.Trim(),
                                Language = "ro/en",
                                IsLiveSource = true
                            });
                        }
                    }
                }
            }
        }
        catch
        {
            // Network errors are swallowed gracefully without breaking UI
        }

        return results;
    }
}
