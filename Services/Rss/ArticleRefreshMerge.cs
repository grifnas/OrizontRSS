namespace CititorRSS.Jaws;

/// <summary>
/// Îmbină articolele descărcate prin RSS cu cele păstrate local. Fluxurile RSS publică
/// doar ultimele articole, deci un articol ieșit din flux rămâne local; singura ștergere
/// după vechime este regula de păstrare (ArticleRetention).
/// </summary>
public static class ArticleRefreshMerge
{
    public sealed record Result(List<Article> Articles, int NewlyReceived, int IgnoredByRetention);

    public static Result Merge(IReadOnlyList<Article> existing, IReadOnlyList<Article> downloaded, DateTimeOffset? retentionCutoff)
    {
        var old = existing
            .Where(article => !string.IsNullOrWhiteSpace(article.Id))
            .GroupBy(article => article.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        foreach (var article in downloaded)
        {
            if (old.TryGetValue(article.Id, out var previous)) CopyLocalState(previous, article);
        }

        var accepted = retentionCutoff is { } cutoff
            ? downloaded.Where(article => article.Published >= cutoff || (old.TryGetValue(article.Id, out var previous) && ArticleRetention.IsProtected(previous))).ToList()
            : downloaded.ToList();
        var ignoredByRetention = downloaded.Count - accepted.Count;
        var newlyReceived = accepted.Count(article => !old.ContainsKey(article.Id));

        var acceptedIds = accepted.Select(article => article.Id).Where(id => !string.IsNullOrWhiteSpace(id)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var downloadedIds = downloaded.Select(article => article.Id).Where(id => !string.IsNullOrWhiteSpace(id)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        accepted.AddRange(existing.Where(article =>
            !acceptedIds.Contains(article.Id) && ShouldKeepExisting(article, downloadedIds, retentionCutoff)));
        return new Result(accepted, newlyReceived, ignoredByRetention);
    }

    private static bool ShouldKeepExisting(Article article, HashSet<string> downloadedIds, DateTimeOffset? retentionCutoff)
    {
        // Articolele NewsBlur rămân pentru sincronizarea pe mai multe calculatoare.
        if (!string.IsNullOrWhiteSpace(article.NewsBlurStoryHash)) return true;
        if (ArticleRetention.IsProtected(article)) return true;
        // Încă în flux, dar ignorat acum de regula de păstrare.
        if (downloadedIds.Contains(article.Id)) return false;
        return retentionCutoff is not { } cutoff || article.Published >= cutoff;
    }

    private static void CopyLocalState(Article previous, Article article)
    {
        article.IsRead = previous.IsRead;
        article.IsFavorite = previous.IsFavorite;
        article.ReadLater = previous.ReadLater;
        article.Tags = previous.Tags ?? [];
        article.AiNotes = previous.AiNotes ?? [];
        article.FullContent = previous.FullContent;
        article.AutomationPendingNewsBlurBaseline = previous.AutomationPendingNewsBlurBaseline;
        article.AutomationAddedReadLater = previous.AutomationAddedReadLater;
        article.NewsBlurStoryHash = previous.NewsBlurStoryHash;
        article.NewsBlurLastRead = previous.NewsBlurLastRead;
        article.NewsBlurLastStarred = previous.NewsBlurLastStarred;
        article.NewsBlurLastTags = previous.NewsBlurLastTags;
        article.NewsBlurLastLocalRead = previous.NewsBlurLastLocalRead;
        article.NewsBlurLastLocalStarred = previous.NewsBlurLastLocalStarred;
        article.NewsBlurLastLocalTags = previous.NewsBlurLastLocalTags;
        article.NewsBlurLastLocalSaved = previous.NewsBlurLastLocalSaved;
    }
}
