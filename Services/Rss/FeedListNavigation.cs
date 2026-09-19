namespace CititorRSS.Jaws;

/// <summary>Stable ordering and keyboard type-ahead helpers for the feed list.</summary>
public static class FeedListNavigation
{
    private static char NormalizeInitial(char value)
    {
        var decomposed = value.ToString().Normalize(System.Text.NormalizationForm.FormD);
        foreach (var character in decomposed)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark)
                return char.ToUpper(character, System.Globalization.CultureInfo.CurrentCulture);
        return char.ToUpper(value, System.Globalization.CultureInfo.CurrentCulture);
    }

    public static IReadOnlyList<Feed> Order(IEnumerable<Feed> feeds) =>
        feeds
            .OrderBy(feed => feed.Name?.Trim() ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(feed => feed.Folder?.Trim() ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(feed => feed.Url?.Trim() ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    /// <summary>
    /// Finds the next feed beginning with <paramref name="initial"/>, starting after
    /// the current row and wrapping at the end like Windows Explorer.
    /// </summary>
    private static int FindNext<T>(IReadOnlyList<T> items, char initial, int selectedIndex, Func<T, string?> nameSelector)
    {
        if (items.Count == 0) return -1;
        var normalized = NormalizeInitial(initial);
        var start = selectedIndex >= 0 && selectedIndex < items.Count ? selectedIndex + 1 : 0;
        for (var offset = 0; offset < items.Count; offset++)
        {
            var index = (start + offset) % items.Count;
            var name = nameSelector(items[index])?.Trim();
            if (!string.IsNullOrEmpty(name) && NormalizeInitial(name[0]) == normalized)
                return index;
        }
        return -1;
    }

    public static int FindNextByInitial(IReadOnlyList<Feed> feeds, char initial, int selectedIndex) =>
        FindNext(feeds, initial, selectedIndex, feed => feed.Name);

    public static int FindNextArticleByInitial(IReadOnlyList<Article> articles, char initial, int selectedIndex) =>
        FindNext(articles, initial, selectedIndex, article => article.Title);
}
