namespace CititorRSS.Jaws;

public sealed record FeedOverlap(Feed First, Feed Second, int CommonArticles, int SmallerFeedArticles, int OverlapPercent);

public sealed record DuplicateAnalysis(int FeedGroups, int ExtraFeedCopies, int DuplicateArticleCopies, IReadOnlyList<FeedOverlap> OverlappingFeeds)
{
    public bool HasDuplicates => ExtraFeedCopies > 0 || DuplicateArticleCopies > 0 || OverlappingFeeds.Count > 0;
    public bool HasCleanableDuplicates => ExtraFeedCopies > 0 || DuplicateArticleCopies > 0;
}

public sealed record DuplicateCleanupResult(int RemovedFeedCopies, int RemovedArticleCopies);

public static class DuplicateCleaner
{
    public static string FeedKey(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return string.Empty;
        if (!Uri.TryCreate(address.Trim(), UriKind.Absolute, out var uri)) return address.Trim();
        var host = uri.IdnHost.TrimEnd('.').ToLowerInvariant();
        var port = uri.IsDefaultPort ? string.Empty : $":{uri.Port}";
        var path = string.IsNullOrEmpty(uri.AbsolutePath) ? "/" : uri.AbsolutePath;
        if (path.Length > 1) path = path.TrimEnd('/');
        return $"{host}{port}{path}{uri.Query}";
    }

    public static DuplicateAnalysis Analyze(IEnumerable<Feed> feeds)
    {
        var list = feeds.ToList();
        var feedGroups = list.Where(feed => !string.IsNullOrWhiteSpace(FeedKey(feed.Url)))
            .GroupBy(feed => FeedKey(feed.Url), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .ToList();
        var extraFeeds = feedGroups.Sum(group => group.Count() - 1);
        var duplicateArticles = list.Sum(feed => CountDuplicateArticles(feed.Articles ?? []));
        foreach (var group in feedGroups)
        {
            var allArticles = group.SelectMany(feed => feed.Articles ?? []).ToList();
            duplicateArticles += CountDuplicateArticles(allArticles) - group.Sum(feed => CountDuplicateArticles(feed.Articles ?? []));
        }
        return new DuplicateAnalysis(feedGroups.Count, extraFeeds, Math.Max(0, duplicateArticles), FindOverlappingFeeds(list));
    }

    public static DuplicateCleanupResult Clean(List<Feed> feeds)
    {
        var removedFeeds = 0;
        var removedArticles = 0;
        var groups = feeds.Where(feed => !string.IsNullOrWhiteSpace(FeedKey(feed.Url)))
            .GroupBy(feed => FeedKey(feed.Url), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.ToList())
            .ToList();

        foreach (var group in groups)
        {
            var primary = group[0];
            var merged = MergeArticles(group.SelectMany(feed => feed.Articles ?? []), out var removedFromGroup);
            removedArticles += removedFromGroup;
            primary.Articles = merged;
            primary.AddedOn = group.Min(feed => feed.AddedOn);
            primary.LastSuccessfulUpdate = Latest(group.Select(feed => feed.LastSuccessfulUpdate));
            primary.LastArticleReceivedOn = Latest(group.Select(feed => feed.LastArticleReceivedOn));
            primary.ConsecutiveFailures = group.Min(feed => feed.ConsecutiveFailures);
            if (primary.ConsecutiveFailures == 0) primary.LastError = null;
            foreach (var duplicate in group.Skip(1))
            {
                feeds.Remove(duplicate);
                removedFeeds++;
            }
        }

        foreach (var feed in feeds)
        {
            feed.Articles ??= [];
            feed.Articles = MergeArticles(feed.Articles, out var removedFromFeed);
            removedArticles += removedFromFeed;
        }
        return new DuplicateCleanupResult(removedFeeds, removedArticles);
    }

    public static List<Article> DeduplicateArticles(IEnumerable<Article> articles, out int removed) => MergeArticles(articles, out removed);

    public static int MergeFeedPair(List<Feed> feeds, Feed keep, Feed remove)
    {
        if (ReferenceEquals(keep, remove) || !feeds.Contains(keep) || !feeds.Contains(remove)) return 0;
        keep.Articles = MergeArticles((keep.Articles ?? []).Concat(remove.Articles ?? []), out var removedArticles);
        keep.AddedOn = keep.AddedOn <= remove.AddedOn ? keep.AddedOn : remove.AddedOn;
        keep.LastSuccessfulUpdate = Latest([keep.LastSuccessfulUpdate, remove.LastSuccessfulUpdate]);
        keep.LastArticleReceivedOn = Latest([keep.LastArticleReceivedOn, remove.LastArticleReceivedOn]);
        keep.ConsecutiveFailures = Math.Min(keep.ConsecutiveFailures, remove.ConsecutiveFailures);
        if (keep.ConsecutiveFailures == 0) keep.LastError = null;
        feeds.Remove(remove);
        return removedArticles;
    }

    private static List<FeedOverlap> FindOverlappingFeeds(List<Feed> feeds)
    {
        var identities = feeds.ToDictionary(
            feed => feed,
            feed => (feed.Articles ?? []).Where(article => !string.IsNullOrWhiteSpace(article.Link)).Select(ArticleKey).ToHashSet(StringComparer.OrdinalIgnoreCase));
        var result = new List<FeedOverlap>();
        for (var firstIndex = 0; firstIndex < feeds.Count; firstIndex++)
        {
            for (var secondIndex = firstIndex + 1; secondIndex < feeds.Count; secondIndex++)
            {
                var first = feeds[firstIndex];
                var second = feeds[secondIndex];
                if (string.Equals(FeedKey(first.Url), FeedKey(second.Url), StringComparison.OrdinalIgnoreCase)) continue;
                var firstArticles = identities[first];
                var secondArticles = identities[second];
                var smallerCount = Math.Min(firstArticles.Count, secondArticles.Count);
                if (smallerCount < 5) continue;
                var common = firstArticles.Count <= secondArticles.Count
                    ? firstArticles.Count(secondArticles.Contains)
                    : secondArticles.Count(firstArticles.Contains);
                var overlap = (int)Math.Round(common * 100d / smallerCount);
                if (common >= 5 && overlap >= 90) result.Add(new FeedOverlap(first, second, common, smallerCount, overlap));
            }
        }
        return result.OrderByDescending(item => item.OverlapPercent).ThenByDescending(item => item.CommonArticles).ToList();
    }

    private static int CountDuplicateArticles(IEnumerable<Article> articles)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicates = 0;
        foreach (var article in articles)
        {
            var key = ArticleKey(article);
            if (!keys.Add(key)) duplicates++;
        }
        return duplicates;
    }

    private static List<Article> MergeArticles(IEnumerable<Article> articles, out int removed)
    {
        var result = new List<Article>();
        var byKey = new Dictionary<string, Article>(StringComparer.OrdinalIgnoreCase);
        removed = 0;
        foreach (var article in articles.OrderByDescending(article => article.Published))
        {
            var key = ArticleKey(article);
            if (!byKey.TryGetValue(key, out var primary))
            {
                byKey[key] = article;
                result.Add(article);
                continue;
            }
            MergeArticle(primary, article);
            removed++;
        }
        return result.OrderByDescending(article => article.Published).ToList();
    }

    public static string ArticleKey(Article article)
    {
        if (!string.IsNullOrWhiteSpace(article.Link)) return $"link:{FeedKey(article.Link)}";
        if (!string.IsNullOrWhiteSpace(article.Id)) return $"id:{article.Id.Trim()}";
        var title = string.Join(' ', (article.Title ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return $"title:{title}|published:{article.Published.UtcDateTime:yyyyMMddHHmm}";
    }

    public static List<Article> DistinctForDisplay(IEnumerable<Article> articles)
    {
        return articles
            .GroupBy(ArticleKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(DisplayPreference)
                .ThenByDescending(article => article.Published)
                .First())
            .ToList();
    }

    private static int DisplayPreference(Article article)
    {
        var score = article.IsRead ? 0 : 8;
        if (article.IsFavorite) score += 4;
        if (article.ReadLater) score += 2;
        if (!string.IsNullOrWhiteSpace(article.FullContent)) score += 1;
        return score;
    }

    private static void MergeArticle(Article primary, Article duplicate)
    {
        primary.IsRead = primary.IsRead && duplicate.IsRead;
        primary.IsFavorite |= duplicate.IsFavorite;
        primary.ReadLater |= duplicate.ReadLater;
        primary.Tags = (primary.Tags ?? []).Concat(duplicate.Tags ?? []).Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(tag => tag).ToList();
        primary.AiNotes = (primary.AiNotes ?? []).Concat(duplicate.AiNotes ?? [])
            .GroupBy(note => $"{note.Title}\n{note.Content}", StringComparer.Ordinal)
            .Select(group => group.OrderBy(note => note.CreatedAt).First())
            .OrderBy(note => note.CreatedAt)
            .ToList();
        if ((duplicate.FullContent?.Length ?? 0) > (primary.FullContent?.Length ?? 0)) primary.FullContent = duplicate.FullContent;
        if ((duplicate.Content?.Length ?? 0) > (primary.Content?.Length ?? 0)) primary.Content = duplicate.Content ?? string.Empty;
        if (string.IsNullOrWhiteSpace(primary.Link)) primary.Link = duplicate.Link;
        if (string.IsNullOrWhiteSpace(primary.Id)) primary.Id = duplicate.Id;
        if (string.IsNullOrWhiteSpace(primary.Title)) primary.Title = duplicate.Title;
    }

    private static DateTimeOffset? Latest(IEnumerable<DateTimeOffset?> values)
    {
        var available = values.Where(value => value.HasValue).Select(value => value!.Value).ToList();
        return available.Count == 0 ? null : available.Max();
    }
}
