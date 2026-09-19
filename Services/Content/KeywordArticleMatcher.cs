using System;
using System.Collections.Generic;
using System.Linq;

namespace CititorRSS.Jaws;

public readonly record struct KeywordArticleMatch(Article Article, string Keyword, string FeedName);

public static class KeywordArticleMatcher
{
    public const int MaximumResults = 3;

    public static IReadOnlyList<KeywordArticleMatch> FindMatchesInFeeds(
        IEnumerable<Feed> feeds,
        string? keywordAlerts)
    {
        var matches = new List<KeywordArticleMatch>(MaximumResults);
        var candidates = feeds
            .Where(feed => feed.Articles is not null)
            .SelectMany(feed => feed.Articles.Select(article => (Feed: feed, Article: article)))
            .OrderByDescending(candidate => candidate.Article.Published);

        foreach (var candidate in candidates)
        {
            AppendMatches(matches, FindMatches([candidate.Article], keywordAlerts, candidate.Feed.Name));
            if (matches.Count >= MaximumResults) break;
        }

        return matches;
    }

    public static IReadOnlyList<KeywordArticleMatch> FindMatches(
        IEnumerable<Article> newArticles,
        string? keywordAlerts,
        string feedName)
    {
        var keywords = keywordAlerts?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .ToArray() ?? [];
        if (keywords.Length == 0) return [];

        var matches = new List<KeywordArticleMatch>(MaximumResults);
        foreach (var article in newArticles)
        {
            if (matches.Count >= MaximumResults) break;
            var title = ArticleSearch.Normalize(ArticleTextNormalizer.ToReadableText(article.Title));
            var content = ArticleSearch.Normalize(ArticleTextNormalizer.ToReadableText(article.Content));
            foreach (var keyword in keywords)
            {
                var normalizedKeyword = ArticleSearch.Normalize(keyword);
                if (!title.Contains(normalizedKeyword, StringComparison.Ordinal) &&
                    !content.Contains(normalizedKeyword, StringComparison.Ordinal))
                    continue;

                matches.Add(new KeywordArticleMatch(article, keyword, feedName));
                break;
            }
        }
        return matches;
    }

    public static void AppendMatches(List<KeywordArticleMatch> destination, IEnumerable<KeywordArticleMatch> matches)
    {
        foreach (var match in matches)
        {
            if (destination.Count >= MaximumResults) break;
            destination.Add(match);
        }
    }
}
