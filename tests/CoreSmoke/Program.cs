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
Check(NewsBlurMetadataConflictPolicy.IsConflict(true, true, "Nume local", "Nume NewsBlur", StringComparison.Ordinal), "modificările concurente cu valori diferite cer alegere explicită");
Check(!NewsBlurMetadataConflictPolicy.IsConflict(true, true, "Același nume", "Același nume", StringComparison.Ordinal), "modificările concurente convergente nu sunt conflict");
Check(!NewsBlurMetadataConflictPolicy.IsConflict(true, true, "Tehnologie", "tehnologie", StringComparison.CurrentCultureIgnoreCase), "diferențele de majuscule din folder nu creează conflict");
Check(!NewsBlurMetadataConflictPolicy.IsConflict(true, false, "Nume local", "Nume vechi", StringComparison.Ordinal), "schimbarea într-o singură parte se sincronizează fără dialog de conflict");
Check(NewsBlurBootstrapPolicy.RequiresInitialImport("grifnas", null), "profilul nou începe cu import unidirecțional din NewsBlur");
Check(!NewsBlurBootstrapPolicy.RequiresInitialImport("Grifnas", "grifnas"), "inițializarea completă este păstrată pentru același cont indiferent de majuscule");
Check(NewsBlurBootstrapPolicy.RequiresInitialImport("alt-cont", "grifnas"), "schimbarea contului NewsBlur cere inițializare separată");
Check(NewsBlurBootstrapPolicy.HasExistingRemoteLink([new Feed { NewsBlurFeedId = "42" }], [new NewsBlurFeedInfo("42", "Exemplu", "https://example.test/rss", "Tehnologie")]), "instalarea deja asociată este recunoscută și trece la flux bidirecțional");
Check(!NewsBlurBootstrapPolicy.HasExistingRemoteLink([new Feed { NewsBlurFeedId = "42" }], [new NewsBlurFeedInfo("84", "Exemplu", "https://example.test/rss", "Tehnologie")]), "un profil curat nu este confundat cu o instalare sincronizată anterior");
Check(!NewsBlurBootstrapPolicy.HasExistingRemoteLink([new Feed { IsDemo = true, NewsBlurFeedId = "42" }], [new NewsBlurFeedInfo("42", "Exemplu", "https://example.test/rss", "Tehnologie")]), "un feed demonstrativ nu marchează profilul drept sincronizat anterior");
var matchedNewsBlurSubscriptions = new NewsBlurSubscription[]
{
    new("Exemplu", "https://EXAMPLE.test:443/rss/", "Tehnologie"),
    new("Știri", "https://news.example.test/feed?format=rss", "Neorganizate")
};
var matchedNewsBlurIndex = new NewsBlurFeedInfo[]
{
    new("42", "Exemplu", "https://example.test/rss", "Tehnologie"),
    new("84", "Știri", "https://news.example.test/feed?format=rss", "Neorganizate")
};
Check(NewsBlurFeedSnapshotPolicy.IsComplete(matchedNewsBlurSubscriptions, matchedNewsBlurIndex, out _), "snapshoturile complete OPML și feed-index sunt validate cu URL-uri normalizate");
Check(NewsBlurFeedSnapshotPolicy.IsComplete([], [], out _), "un cont NewsBlur gol este o stare validă numai când ambele liste sunt goale");
Check(!NewsBlurFeedSnapshotPolicy.IsComplete(matchedNewsBlurSubscriptions, matchedNewsBlurIndex[..1], out _), "un index NewsBlur incomplet blochează sincronizarea și ștergerile");
Check(!NewsBlurFeedSnapshotPolicy.IsComplete(matchedNewsBlurSubscriptions, [.. matchedNewsBlurIndex, matchedNewsBlurIndex[0] with { FeedId = "99" }], out _), "adresele repetate în index blochează oglindirea");
var localDuplicateGroups = NewsBlurFeedSnapshotPolicy.FindDuplicateLocalFeeds(
[
    new Feed { Name = "Feed original", Folder = "Știri", Url = "https://example.test/rss/" },
    new Feed { Name = "Copie", Folder = "Arhivă", Url = "https://EXAMPLE.test/rss" },
    new Feed { Name = "Feed unic", Url = "https://other.example.test/rss" }
]);
Check(localDuplicateGroups.Count == 1 && localDuplicateGroups[0].Count == 2, "duplicatele locale normalizate sunt grupate pentru afișarea avertizării NewsBlur");
var cleanupFeedDeletions = NewsBlurFeedSnapshotPolicy.CreateCleanupDeletions(
[
    new Feed { Name = "Feed suprapus eliminat", Url = "https://overlap.example.test/rss", NewsBlurFeedId = "91" },
    new Feed { Name = "Copie cu aceeași adresă", Url = "https://EXAMPLE.test:443/rss", NewsBlurFeedId = "92" }
],
[
    new Feed { Name = "Feed păstrat", Url = "https://example.test/rss/", NewsBlurFeedId = "42" }
]);
Check(cleanupFeedDeletions.Count == 1 && cleanupFeedDeletions[0].FeedId == "91", "curățarea programează dezabonarea NewsBlur doar pentru feedul suprapus eliminat cu adresă distinctă");
Check(NewsBlurFeedSnapshotPolicy.IsValidPendingDeletion(new NewsBlurPendingFeedDeletion { Url = "https://overlap.example.test/rss" }), "dezabonarea confirmată prin curățare poate fi rezolvată după adresă când lipsește ID-ul NewsBlur local");
Check(!NewsBlurFeedSnapshotPolicy.IsValidPendingDeletion(new NewsBlurPendingFeedDeletion()), "o cerere de dezabonare fără ID și fără adresă validă este respinsă");
Check(NewsBlurFeedSnapshotPolicy.IsRemoteDeletion(new Feed { NewsBlurFeedId = "42", Url = "https://example.test/rss" }, []), "lipsa unui feed asociat din snapshotul complet reprezintă o ștergere remote");
Check(!NewsBlurFeedSnapshotPolicy.IsRemoteDeletion(new Feed { NewsBlurFeedId = "42", Url = "https://example.test/rss" }, [new NewsBlurFeedInfo("99", "Reabonat", "https://example.test/rss", "Neorganizate")]), "reabonarea aceleiași adrese remote nu se confundă cu ștergerea");
Check(!NewsBlurFeedSnapshotPolicy.IsRemoteDeletion(new Feed { Url = "https://example.test/rss" }, []), "un feed local neasociat nu este șters doar pentru că lipsește remote");
Check(!NewsBlurBootstrapPolicy.IsSyncEligible(new Feed { IsDemo = true }), "feedurile demonstrative nu se trimit sau sincronizează ca date reale");
Check(NewsBlurBootstrapPolicy.IsSyncEligible(new Feed { Url = "https://example.test/rss" }), "feedurile reale rămân eligibile pentru sincronizarea bidirecțională");
Check(NewsBlurFolderMapping.IsUnorganized(" neorganizate ") && !NewsBlurFolderMapping.IsNamedFolder("Neorganizate"), "Neorganizate este o categorie specială, nu folder NewsBlur propriu-zis");
Check(NewsBlurFolderMapping.ToNewsBlurFolder("Neorganizate") == string.Empty && NewsBlurFolderMapping.FromNewsBlurFolder(null) == "Neorganizate", "feedurile fără folder NewsBlur se mapează la Neorganizate");
var moveToRoot = NewsBlurFolderMapping.CreateMoveFeedParameters("42", "Știri", "Neorganizate");
Check(moveToRoot.Single(item => item.Key == "in_folder").Value == "Știri" && moveToRoot.Single(item => item.Key == "to_folder").Value == string.Empty, "ștergerea folderului mută feedurile la nivelul superior NewsBlur, nu le dezabonează");
var moveFromRoot = NewsBlurFolderMapping.CreateMoveFeedParameters("42", "Neorganizate", "Știri");
Check(moveFromRoot.Single(item => item.Key == "in_folder").Value == string.Empty && moveFromRoot.Single(item => item.Key == "to_folder").Value == "Știri", "mutarea unui feed neorganizat într-un folder folosește nivelul superior ca sursă");

var cutoff = now.AddDays(-90);
var old = new Article { Id = "old", Published = now.AddDays(-100) };
var favoriteOld = new Article { Id = "favorite", Published = now.AddDays(-100), IsFavorite = true };
var laterOld = new Article { Id = "later", Published = now.AddDays(-100), ReadLater = true };
var retentionFeed = new Feed { Articles = [old, favoriteOld, laterOld] };
Check(ArticleRetention.CountExpired([retentionFeed], cutoff) == 1, "retenția numără numai articolul obișnuit expirat");
Check(ArticleRetention.RemoveExpired([retentionFeed], cutoff) == 1 && retentionFeed.Articles.Count == 2, "retenția păstrează favoritele și articolele pentru mai târziu");
Check(!ArticleRetention.ShouldKeep(now.AddDays(-100), false, false, cutoff), "articolele NewsBlur obișnuite expirate nu se reimportă în Orizont");
Check(ArticleRetention.ShouldKeep(now.AddDays(-100), true, false, cutoff) && ArticleRetention.ShouldKeep(now.AddDays(-100), false, true, cutoff), "articolele NewsBlur salvate ca favorite sau Mai târziu rămân protejate de retenție");

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
    ReaderWideSpacing = true,
    SpeechEngine = SpeechEngineIds.EspeakNg,
    EspeakVoiceName = "ro",
    EspeakVariant = "max",
    EspeakInflection = 75,
    NewsBlurPendingFeedDeletions =
    [
        new NewsBlurPendingFeedDeletion { FeedId = "42", Url = "https://example.test/rss", Name = "Exemplu", Folder = "Tehnologie" }
    ],
    NewsBlurBootstrapCompletedUsername = "grifnas"
};
var sanitized = BackupPolicy.SanitizeSettings(settings);
var backupJson = JsonSerializer.Serialize(new BackupDocument { Feeds = feeds, Settings = sanitized });
Check(!sanitized.GeminiEnabled && sanitized.EncryptedGeminiKey is null, "backupul dezactivează și elimină cheia Gemini");
Check(!backupJson.Contains("ENCRYPTED-SECRET", StringComparison.Ordinal), "cheia Gemini nu apare în JSON-ul backupului");
Check(sanitized.ReaderWindowWidth == 1234 && sanitized.ReaderWideSpacing, "backupul păstrează setările de afișare");
Check(sanitized.EspeakVoiceName == "ro" && sanitized.EspeakVariant == "max" && sanitized.EspeakInflection == 75, "backupul păstrează limba, varianta și intonația eSpeak");
Check(sanitized.NewsBlurPendingFeedDeletions.Count == 1 && sanitized.NewsBlurPendingFeedDeletions[0].FeedId == "42", "backupul păstrează coada de dezabonări confirmate fără sesiunea NewsBlur");
Check(sanitized.NewsBlurBootstrapCompletedUsername is null, "backupul nu transferă marcajul de inițializare între calculatoare");

var localDemo = DemoFeedCatalog.CreateLocalFeed("ro-RO");
Check(DemoFeedCatalog.IsLocal(localDemo) && DemoFeedCatalog.IsDemo(localDemo), "catalogul creează feedul demonstrativ local");
Check(localDemo.Articles.Count == 3 && localDemo.Articles[1].IsFavorite && localDemo.Articles[2].ReadLater, "feedul local include articole pentru testarea marcajelor");
foreach (var language in new[] { "ro-RO", "en-US", "es-ES", "fr-FR", "de-DE", "pt-BR", "hu-HU", "it-IT" })
{
    var online = DemoFeedCatalog.OnlineForLanguage(language);
    Check(online is not null && Uri.TryCreate(online.Url, UriKind.Absolute, out var onlineUri) && onlineUri.Scheme is "http" or "https", $"catalogul RSS include un feed online valid pentru {language}");
}
Check(DemoFeedCatalog.OnlineForLanguage("ro-RO")?.Url == "https://hotnews.ro/feed", "exemplul RSS românesc folosește endpointul HotNews care răspunde ca RSS");

Console.WriteLine($"Core smoke test passed: {checks} verificări, 1.200 articole, retenție, duplicate, backup și căutare.");
