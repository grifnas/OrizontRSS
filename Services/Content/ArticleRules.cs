using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CititorRSS.Jaws;

/// <summary>Additive rules over already available text; no I/O and no remote state changes.</summary>
public static class ArticleRules
{
    private static string Normalize(string? text)
    {
        var result = new StringBuilder();
        foreach (var c in (text ?? "").Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                result.Append(char.ToUpperInvariant(c));
        return Regex.Replace(result.ToString(), @"\s+", " ").Trim();
    }

    private static bool ContainsTerm(string text, string term)
    {
        var start = 0;
        while ((start = text.IndexOf(term, start, StringComparison.Ordinal)) >= 0)
        {
            var end = start + term.Length;
            if ((start == 0 || !char.IsLetterOrDigit(text[start - 1])) &&
                (end == text.Length || !char.IsLetterOrDigit(text[end]))) return true;
            start++;
        }
        return false;
    }

    public static bool Matches(ArticleRule rule, Feed feed, Article article)
    {
        if (!rule.Enabled || (!rule.AllFeeds && !(rule.FeedIds ?? []).Contains(feed.Id))) return false;
        var terms = (rule.Terms ?? []).Select(Normalize).Where(t => t.Length > 0).Distinct().ToList();
        if (terms.Count == 0 || (!(rule.Tags ?? []).Any(t => !string.IsNullOrWhiteSpace(t)) && !rule.MarkReadLater)) return false;
        var title = Normalize(ArticleTextNormalizer.ToReadableText(article.Title));
        var body = rule.IncludeContent ? Normalize(ArticleTextNormalizer.ToReadableText(article.Content) + " " +
            ArticleTextNormalizer.ToReadableText(article.FullContent)) : "";
        bool Match(string term) => ContainsTerm(title, term) || (rule.IncludeContent && ContainsTerm(body, term));
        return rule.MatchAll ? terms.All(Match) : terms.Any(Match);
    }

    public static bool Apply(IEnumerable<ArticleRule> rules, Feed feed, Article article)
    {
        var changed = false;
        foreach (var rule in rules.Where(r => r is not null && Matches(r, feed, article)))
        {
            foreach (var tag in (rule.Tags ?? []).Select(t => t.Trim()).Where(t => t.Length is > 0 and <= 50))
            {
                article.Tags ??= [];
                if (article.Tags.Contains(tag, StringComparer.CurrentCultureIgnoreCase)) continue;
                article.Tags.Add(tag);
                changed = true;
            }
            if (rule.MarkReadLater && !article.ReadLater)
            {
                if (!article.NewsBlurLastRead.HasValue) article.AutomationAddedReadLater = true;
                article.ReadLater = true; changed = true;
            }
        }
        if (changed && (!article.NewsBlurLastRead.HasValue || article.NewsBlurLastLocalTags is null)) article.AutomationPendingNewsBlurBaseline = true;
        return changed;
    }

    // Store hashed identities, including IDs and URLs, so redownloads do not undo a manual removal.
    private static IEnumerable<string> Keys(Article a)
    {
        foreach (var value in new[] { a.Id, a.Link, a.NewsBlurStoryHash }.Where(v => !string.IsNullOrWhiteSpace(v)))
            yield return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value!.Trim().ToUpperInvariant())));
        if (string.IsNullOrWhiteSpace(a.Id) && string.IsNullOrWhiteSpace(a.Link) && string.IsNullOrWhiteSpace(a.NewsBlurStoryHash))
            yield return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(a.Title + "|" + a.Published.ToUniversalTime().ToString("O"))));
    }

    public static void SeedExisting(Feed feed)
    {
        feed.AutomationSeen ??= [];
        foreach (var article in feed.Articles) feed.AutomationSeen.UnionWith(Keys(article));
    }

    public static bool ApplyIncoming(IEnumerable<ArticleRule> rules, Feed feed, Article article)
    {
        feed.AutomationSeen ??= [];
        var keys = Keys(article).ToList();
        var seen = keys.Any(feed.AutomationSeen.Contains);
        feed.AutomationSeen.UnionWith(keys);
        return !seen && Apply(rules, feed, article);
    }
}
