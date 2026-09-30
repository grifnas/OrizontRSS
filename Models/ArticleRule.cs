namespace CititorRSS.Jaws;

public sealed class ArticleRule
{
    public string Name { get; set; } = "";
    public bool Enabled { get; set; }
    public List<string> Terms { get; set; } = [];
    public bool MatchAll { get; set; }
    public bool IncludeContent { get; set; } = true;
    public bool AllFeeds { get; set; } = true;
    public List<Guid> FeedIds { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public bool MarkReadLater { get; set; }
    public ArticleRule Copy() => new()
    {
        Name = Name, Enabled = Enabled, Terms = (Terms ?? []).ToList(),
        MatchAll = MatchAll, IncludeContent = IncludeContent, AllFeeds = AllFeeds,
        FeedIds = (FeedIds ?? []).ToList(), Tags = (Tags ?? []).ToList(), MarkReadLater = MarkReadLater
    };
}
