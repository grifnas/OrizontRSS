using System.Globalization;
using System.Text.RegularExpressions;

namespace CititorRSS.Jaws;

public static class ShortcutSearch
{
    public static bool Matches(string text, string query)
    {
        // Accept both Ctrl+Shift+F8 and Ctrl + Shift + F8, and unordered words.
        var normalizedText = Regex.Replace(text, @"\s*\+\s*", "+");
        var normalizedQuery = Regex.Replace(query, @"\s*\+\s*", "+");
        return normalizedQuery.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .All(term => CultureInfo.CurrentCulture.CompareInfo.IndexOf(normalizedText, term,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0);
    }
}
