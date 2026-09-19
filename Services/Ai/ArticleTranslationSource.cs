namespace CititorRSS.Jaws;

public static class ArticleTranslationSource
{
    private static readonly string[] PageChromeMarkers =
    [
        "PUBLICITATE", "PUBLICIDADE", "ADVERTISEMENT", "WERBUNG", "PUBLICIDAD", "PUBLICITÉ", "HIRDETÉS", "PUBBLICITÀ",
        "ACASĂ", "HOME", "RECENZII", "REVIEWS", "COMPARĂ", "VERGLEICH", "COMPARAR", "COMPARER", "VERGLEICHEN",
        "RSS", "INSTAGRAM", "TIKTOK", "FACEBOOK", "TWITTER", "© 20"
    ];

    public static async Task<string> ResolveAsync(string? knownReadableText, string? fallbackText, Func<Task<string>>? loadReadableText = null, string? articleTitle = null)
    {
        var known = RssReader.CleanReadableContent(knownReadableText, articleTitle);
        var fallback = RssReader.CleanReadableContent(fallbackText, articleTitle);
        if (!string.IsNullOrWhiteSpace(known) && !LooksLikePageChrome(known)) return known;

        if (loadReadableText is not null)
        {
            try
            {
                var loaded = await loadReadableText();
                if (!string.IsNullOrWhiteSpace(loaded)) return RssReader.CleanReadableContent(loaded, articleTitle);
            }
            catch
            {
                // Translation can still use the RSS content when the page cannot be refreshed.
            }
        }

        return !string.IsNullOrWhiteSpace(fallback) ? fallback : known;
    }

    private static bool LooksLikePageChrome(string text)
    {
        var matches = PageChromeMarkers.Count(marker => text.Contains(marker, StringComparison.OrdinalIgnoreCase));
        return matches >= 2;
    }
}
