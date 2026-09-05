using System.Text.Json;
using CititorRSS.Jaws;

var checks = 0;
void Check(bool condition, string description)
{
    checks++;
    if (!condition) throw new InvalidOperationException($"Eșec: {description}");
}

var now = DateTimeOffset.UtcNow;
var feeds = Enumerable.Range(1, 5).Select(feedNumber => new Feed
{
    Name = $"Feed {feedNumber}",
    Url = $"https://example{feedNumber}.test/rss",
    Folder = feedNumber % 2 == 0 ? "Tehnologie" : "Actualitate",
    Articles = Enumerable.Range(1, 240).Select(articleNumber => new Article
    {
        Id = $"feed-{feedNumber}-article-{articleNumber}",
        Title = articleNumber == 17 ? "Călin Georgescu despre tehnologie" : $"Știre {articleNumber}",
        Content = articleNumber == 17 ? "Analiză despre tehnologie și actualitate" : "Conținut de test",
        Link = $"https://example{feedNumber}.test/{articleNumber}",
        Published = now.AddMinutes(-articleNumber)
    }).ToList()
}).ToList();

Check(feeds.SelectMany(feed => feed.Articles).Count() == 1200, "setul de stres conține 1.200 de articole");
Check(ArticleSearch.Matches(feeds[0].Articles[16], "calin georgescu"), "căutarea fără diacritice și cu două cuvinte");
Check(ArticleSearch.Matches(feeds[0].Articles[16], "tehnologie actualitate"), "căutarea cu mai multe cuvinte în titlu/conținut");
Check(!ArticleSearch.Matches(feeds[0].Articles[0], "cuvânt inexistent"), "căutarea nu returnează rezultate false");
Check(DuplicateCleaner.DistinctForDisplay(feeds.SelectMany(feed => feed.Articles)).Count() == 1200, "vederea globală nu limitează lista la 10 articole");

var cutoff = now.AddDays(-90);
var old = new Article { Id = "old", Published = now.AddDays(-100) };
var favoriteOld = new Article { Id = "favorite", Published = now.AddDays(-100), IsFavorite = true };
var laterOld = new Article { Id = "later", Published = now.AddDays(-100), ReadLater = true };
var retentionFeed = new Feed { Articles = [old, favoriteOld, laterOld] };
Check(ArticleRetention.CountExpired([retentionFeed], cutoff) == 1, "retenția numără numai articolul obișnuit expirat");
Check(ArticleRetention.RemoveExpired([retentionFeed], cutoff) == 1 && retentionFeed.Articles.Count == 2, "retenția păstrează favoritele și articolele pentru mai târziu");

var attentionByErrors = new Feed { ConsecutiveFailures = 3, AddedOn = now };
var attentionBySilence = new Feed { AddedOn = now.AddDays(-100), LastArticleReceivedOn = now.AddDays(-91) };
Check(attentionByErrors.NeedsAttention && attentionByErrors.AttentionReason.Length > 0, "feedul cu trei erori necesită atenție");
Check(attentionBySilence.NeedsAttention && attentionBySilence.HasThreeMonthSilence, "feedul fără articole timp de trei luni necesită atenție");

var duplicateArticleA = new Article { Id = "same", Title = "A", Link = "https://example.test/a", Published = now, IsFavorite = true, Tags = ["important"] };
var duplicateArticleB = new Article { Id = "same", Title = "A", Link = "https://example.test/a", Published = now.AddMinutes(-1), ReadLater = true, Tags = ["de verificat"] };
var duplicateFeeds = new List<Feed>
{
    new() { Name = "Principal", Url = "https://example.test/rss/", Articles = [duplicateArticleA] },
    new() { Name = "Copie", Url = "https://EXAMPLE.test:443/rss", Articles = [duplicateArticleB] }
};
var analysis = DuplicateCleaner.Analyze(duplicateFeeds);
Check(analysis.FeedGroups == 1 && analysis.ExtraFeedCopies == 1, "duplicatele de feed sunt detectate");
var cleanup = DuplicateCleaner.Clean(duplicateFeeds);
Check(cleanup.RemovedFeedCopies == 1 && duplicateFeeds.Count == 1, "duplicatele de feed sunt comasate");
Check(duplicateFeeds[0].Articles.Count == 1 && duplicateFeeds[0].Articles[0].IsFavorite && duplicateFeeds[0].Articles[0].ReadLater, "comasarea păstrează marcajele articolului");
Check(duplicateFeeds[0].Articles[0].Tags.Count == 2, "comasarea păstrează etichetele articolului");

var settings = new AppSettings
{
    GeminiEnabled = true,
    EncryptedGeminiKey = "ENCRYPTED-SECRET",
    ReaderWindowWidth = 1234,
    ReaderWindowHeight = 777,
    ReaderFontSize = 22,
    ReaderWideSpacing = true
};
var sanitized = BackupPolicy.SanitizeSettings(settings);
var backupJson = JsonSerializer.Serialize(new BackupDocument { Feeds = feeds, Settings = sanitized });
Check(!sanitized.GeminiEnabled && sanitized.EncryptedGeminiKey is null, "backupul dezactivează și elimină cheia Gemini");
Check(!backupJson.Contains("ENCRYPTED-SECRET", StringComparison.Ordinal), "cheia Gemini nu apare în JSON-ul backupului");
Check(sanitized.ReaderWindowWidth == 1234 && sanitized.ReaderWideSpacing, "backupul păstrează setările de afișare");

Console.WriteLine($"Core smoke test passed: {checks} verificări, 1.200 articole, retenție, duplicate, backup și căutare.");
