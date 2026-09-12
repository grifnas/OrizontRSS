namespace CititorRSS.Jaws;

/// <summary>Regulile de păstrare a articolelor obișnuite.</summary>
public static class ArticleRetention
{
    public static bool IsProtected(Article article) => article.IsFavorite || article.ReadLater;
    public static bool ShouldKeep(Article article, DateTimeOffset cutoff) =>
        ShouldKeep(article.Published, article.IsFavorite, article.ReadLater, cutoff);

    public static bool ShouldKeep(DateTimeOffset published, bool isFavorite, bool readLater, DateTimeOffset cutoff) =>
        published >= cutoff || isFavorite || readLater;

    public static int CountExpired(IEnumerable<Feed> feeds, DateTimeOffset cutoff) =>
        feeds.Sum(feed => feed.Articles.Count(article => !ShouldKeep(article, cutoff)));

    public static int RemoveExpired(IList<Feed> feeds, DateTimeOffset cutoff)
    {
        var removed = 0;
        foreach (var feed in feeds)
        {
            var kept = feed.Articles.Where(article => ShouldKeep(article, cutoff)).ToList();
            removed += feed.Articles.Count - kept.Count;
            feed.Articles = kept;
        }
        return removed;
    }
}
