using System.Net;
using System.Text.RegularExpressions;

namespace CititorRSS.Jaws;

/// <summary>Extracts readable text from RSS/HTML fragments before article keyword matching.</summary>
internal static class ArticleTextNormalizer
{
    private static readonly Regex NonVisibleBlocks = new(
        @"<(script|style|noscript|svg|template|head)\b[^>]*>[\s\S]*?</\1\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HtmlComments = new(@"<!--[\s\S]*?-->", RegexOptions.Compiled);
    private static readonly Regex HtmlTags = new(@"</?[A-Za-z][^>]*>", RegexOptions.Compiled);
    private static readonly Regex RepeatedWhitespace = new(@"\s+", RegexOptions.Compiled);

    public static string ToReadableText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var text = value;
        for (var pass = 0; pass < 2; pass++)
        {
            var decoded = WebUtility.HtmlDecode(text);
            if (string.Equals(decoded, text, StringComparison.Ordinal)) break;
            text = decoded;
        }

        text = NonVisibleBlocks.Replace(text, " ");
        text = HtmlComments.Replace(text, " ");
        text = HtmlTags.Replace(text, " ");
        text = WebUtility.HtmlDecode(text);
        return RepeatedWhitespace.Replace(text, " ").Trim();
    }
}
