using System.Globalization;
using System.Text;

namespace CititorRSS.Jaws;

/// <summary>Reguli comune pentru căutarea locală, inclusiv mai multe cuvinte și diacritice.</summary>
public static class ArticleSearch
{
    public static bool Matches(Article article, string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;
        var words = Normalize(query).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var searchable = Normalize($"{article.Title} {article.SourceName} {article.Content} {article.Link}");
        return words.All(word => searchable.Contains(word, StringComparison.Ordinal));
    }

    public static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark) builder.Append(character);
        return builder.ToString().ToLowerInvariant();
    }
}
