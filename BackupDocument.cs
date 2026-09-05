namespace CititorRSS.Jaws;

public sealed class BackupDocument
{
    public int FormatVersion { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public List<Feed> Feeds { get; set; } = [];
    public AppSettings Settings { get; set; } = new();
}
