using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CititorRSS.Jaws.Services.Content;
using CititorRSS.Jaws.Services.Update;
using CititorRSS.Jaws.Localization;
using CititorRSS.Jaws;

var checks = 0;
void Check(bool condition, string description)
{
    checks++;
    if (!condition) throw new InvalidOperationException($"Eșec: {description}");
}

var now = DateTimeOffset.UtcNow;
var ruleFeed = new Feed { Name = "Reguli test" };
var autoRule = new ArticleRule { Name = "Android", Enabled = true, Terms = ["android"], Tags = ["Tehnologie"], MarkReadLater = true };
var tagged = new Article { Id = "rule-1", Title = "ANDROID nou", Tags = ["personal"], IsRead = true, IsFavorite = true };
Check(ArticleRules.ApplyIncoming([autoRule], ruleFeed, tagged), "regula aplicată articolului nou");
Check(tagged.ReadLater && tagged.Tags.SequenceEqual(new[] { "personal", "Tehnologie" }) && tagged.IsRead && tagged.IsFavorite, "reguli strict aditive");
tagged.ReadLater = false; tagged.Tags.Remove("Tehnologie");
Check(!ArticleRules.ApplyIncoming([autoRule], ruleFeed, tagged) && !tagged.ReadLater, "excluderea manuală nu este anulată la actualizare");
var persistedFeed = JsonSerializer.Deserialize<Feed>(JsonSerializer.Serialize(ruleFeed))!;
Check(!ArticleRules.ApplyIncoming([autoRule], persistedFeed, tagged), "evidența procesării supraviețuiește repornirii");
var disabled = autoRule.Copy(); disabled.Enabled = false;
Check(!ArticleRules.Apply([disabled], ruleFeed, new Article { Title = "Android" }), "regulă dezactivată");
var wordRule = autoRule.Copy(); wordRule.Terms = ["AI"];
Check(!ArticleRules.Matches(wordRule, ruleFeed, new Article { Title = "Mai multe știri" }), "AI nu se potrivește în mai");
wordRule.Terms = ["inteligenta artificiala"];
Check(ArticleRules.Matches(wordRule, ruleFeed, new Article { Title = "Inteligență artificială!" }), "expresie cu diacritice");
Check(!ArticleRules.Matches(wordRule, ruleFeed, new Article { Title = "Inteligență nouă artificială" }), "expresie întreagă");
wordRule.Terms = ["android", "telefon"]; wordRule.MatchAll = true;
Check(!ArticleRules.Matches(wordRule, ruleFeed, new Article { Title = "Android" }), "toate expresiile obligatorii");
Check(ArticleRules.Matches(wordRule, ruleFeed, new Article { Title = "Android", Content = "<p>Telefon</p>" }), "potrivire în titlu și conținut");
wordRule.IncludeContent = false;
Check(!ArticleRules.Matches(wordRule, ruleFeed, new Article { Title = "Android", Content = "telefon" }), "limitare la titlu");
wordRule = autoRule.Copy(); wordRule.AllFeeds = false;
Check(!ArticleRules.Matches(wordRule, ruleFeed, tagged), "selecția goală de feeduri nu înseamnă toate");
wordRule.FeedIds.Add(ruleFeed.Id);
Check(ArticleRules.Matches(wordRule, ruleFeed, tagged), "feed selectat");
var oldArticle = new Article { Id = "existing", Title = "Android" };
ruleFeed.Articles.Add(oldArticle); ArticleRules.SeedExisting(ruleFeed);
Check(!ArticleRules.ApplyIncoming([autoRule], ruleFeed, oldArticle), "articolele existente nu sunt procesate automat");
Check(ArticleRules.Apply([autoRule], ruleFeed, oldArticle), "aplicare explicită la articol existent");
Check(!ArticleRules.Apply([autoRule], ruleFeed, oldArticle), "aplicare repetată fără etichete duplicate");
var stacked = autoRule.Copy(); stacked.Tags = ["tehnologie", "mobil"];
ArticleRules.Apply([autoRule, stacked], ruleFeed, oldArticle);
Check(oldArticle.Tags.Count == 2 && oldArticle.Tags.Contains("mobil"), "reguli cumulate fără duplicate");
Check(ArticleRetention.ShouldKeep(oldArticle, now.AddDays(1)), "Mai târziu protejează retenția");
var baseline = new Article { Title = "Android", NewsBlurLastLocalTags = ["remote"], Tags = ["remote"], NewsBlurLastLocalSaved = false };
ArticleRules.Apply([autoRule], ruleFeed, baseline);
Check(baseline.NewsBlurLastLocalTags.SequenceEqual(new[] { "remote" }) && baseline.NewsBlurLastLocalSaved == false, "regulile nu alterează baza sincronizării");
var ruleBackup = BackupPolicy.SanitizeSettings(new AppSettings { ArticleRules = [autoRule] });
autoRule.Tags.Add("test copie");
Check(ruleBackup.ArticleRules.Count == 1 && !ruleBackup.ArticleRules[0].Tags.Contains("test copie"), "reguli păstrate și copiate în backup");
autoRule.Tags.Remove("test copie");
Check(!ArticleRules.Matches(autoRule, ruleFeed, new Article { Content = "<script>android</script><p>altceva</p>" }), "nu caută în scripturi HTML");
Check(ArticleRules.Matches(autoRule, ruleFeed, new Article { Content = "<p>Android &amp; telefoane</p>" }), "căutare în text HTML vizibil");
var emptyRule = autoRule.Copy(); emptyRule.Terms = [" ", ""];
Check(!ArticleRules.Matches(emptyRule, ruleFeed, tagged), "regula fără expresii nu se potrivește tuturor");
var onlyTag = autoRule.Copy(); onlyTag.MarkReadLater = false;
var onlyTagged = new Article { Title = "Android" };
ArticleRules.Apply([onlyTag], ruleFeed, onlyTagged);
Check(!onlyTagged.ReadLater && onlyTagged.Tags.Count == 1, "regulă numai pentru etichetă");
var onlyLater = autoRule.Copy(); onlyLater.Tags = [];
var laterArticle = new Article { Title = "Android", NewsBlurLastLocalTags = [] };
ArticleRules.Apply([onlyLater], ruleFeed, laterArticle);
Check(laterArticle.ReadLater && laterArticle.Tags.Count == 0 && laterArticle.AutomationPendingNewsBlurBaseline, "Mai târziu fără etichete și asociere NewsBlur încă absentă");
var changedId = new Article { Id = "another", Link = "https://example.test/same", Title = "Android" };
var firstId = new Article { Id = "first", Link = changedId.Link, Title = "Android" };
ArticleRules.ApplyIncoming([autoRule], ruleFeed, firstId);
Check(!ArticleRules.ApplyIncoming([autoRule], ruleFeed, changedId), "identificator schimbat dar aceeași adresă nu reprocesează");
var seenWhileDisabled = new Article { Id = "disabled-first", Title = "Android" };
ArticleRules.ApplyIncoming([], ruleFeed, seenWhileDisabled);
Check(!ArticleRules.ApplyIncoming([autoRule], ruleFeed, seenWhileDisabled), "activarea ulterioară nu aplică retroactiv");
var mergeA = new Feed { Url = "https://example.test/rules", AutomationSeen = ["A"] };
var mergeB = new Feed { Url = mergeA.Url, AutomationSeen = ["B"] };
var mergeList = new List<Feed> { mergeA, mergeB };
DuplicateCleaner.Clean(mergeList);
Check(mergeList.Count == 1 && mergeList[0].AutomationSeen.SetEquals(new[] { "A", "B" }), "comasarea păstrează istoricul regulilor");
Check(ShortcutSearch.Matches("Cititor Orizont. Ctrl+Shift+F8. Schimbă modul", "ctrl + shift + f8"), "căutare scurtături cu spații în combinație");
Check(ShortcutSearch.Matches("Citire vocală. F9. Citește articolul", "articolul vocala"), "căutare scurtături cu termeni multipli și fără diacritice");
Check(ShortcutSearch.Matches("Citire vocală. F9", "  "), "căutarea goală păstrează scurtăturile");
Check(!ShortcutSearch.Matches("Citire vocală. F9", "vocală export"), "toți termenii căutării trebuie să corespundă");
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
var exportArticle = new Article { Title = "Titlu: test?", Content = "Conținut exportat", Link = "https://example.test/article" };
Check(ArticleExportService.SanitizeFileName(exportArticle.Title) == "Titlu_ test_", "numele fișierului exportat elimină caracterele interzise de Windows");
var plainExport = ArticleExportService.BuildText(exportArticle, exportArticle.Content, "Feed test");
Check(plainExport.Contains(UiText.Translate("Conținut preluat prin Orizont RSS:")) && plainExport.Contains(ArticleSharing.PresentationUrl) &&
      plainExport.Contains(UiText.Translate("Sursa articolului:")) && plainExport.Contains(exportArticle.Link),
    "exportul original păstrează footerul localizat, linkul Orizont RSS și sursa articolului");
var translatedContext = new ArticleDistributionContext(exportArticle.Title, "Original article body", "Translated article body", exportArticle.Link, "DeepL", "română");
var translatedNote = translatedContext.BuildTranslationNote("Traducere automată realizată prin {0} în limba {1}.");
var translatedExport = ArticleExportService.BuildText(exportArticle, translatedContext, "Feed test", "Conținut preluat prin Orizont RSS:", "Sursa articolului:", translatedNote);
Check(translatedExport.Contains("Translated article body") && !translatedExport.Contains("Original article body"), "exportul folosește textul afișat și păstrează separat contextul original");
Check(translatedExport.Contains("Traducere automată realizată prin DeepL în limba română.") && translatedExport.Contains(ArticleSharing.PresentationUrl) && translatedExport.Contains(exportArticle.Link), "exportul tradus păstrează furnizorul, limba și ambele surse");
var restoredContext = translatedContext with { DisplayText = "Original article body", TranslationProvider = null, TranslationLanguage = null };
var restoredNote = restoredContext.BuildTranslationNote("Traducere automată realizată prin {0} în limba {1}.");
var restoredExport = ArticleExportService.BuildText(exportArticle, restoredContext, "Feed test", "Conținut preluat prin Orizont RSS:", "Sursa articolului:", restoredNote);
Check(restoredNote is null && !restoredExport.Contains("Traducere automată realizată"), "exportul original nu păstrează o mențiune învechită de traducere");
var exportTestFolder = Path.Combine(Path.GetTempPath(), "OrizontRSS-CoreSmoke-" + Guid.NewGuid().ToString("N"));
var txtExport = ArticleExportService.Save(exportArticle, exportArticle.Content, exportTestFolder, "Feed test", "txt");
var rtfExport = ArticleExportService.Save(exportArticle, exportArticle.Content, exportTestFolder, "Feed test", "rtf");
var translatedTxtExport = ArticleExportService.Save(exportArticle, translatedContext, exportTestFolder, "Feed test", "txt",
    "Conținut preluat prin Orizont RSS:", "Sursa articolului:", translatedNote);
var translatedRtfExport = ArticleExportService.Save(exportArticle, translatedContext, exportTestFolder, "Feed test", "rtf",
    "Conținut preluat prin Orizont RSS:", "Sursa articolului:", translatedNote);
Check(File.Exists(txtExport) && Path.GetExtension(txtExport) == ".txt", "exportul TXT creează fișierul în folderul configurat");
Check(File.Exists(rtfExport) && Path.GetExtension(rtfExport) == ".rtf", "exportul RTF creează fișierul în folderul configurat");
Check(File.ReadAllText(translatedTxtExport).Contains("Traducere automată realizată prin DeepL în limba română.") && File.ReadAllText(translatedTxtExport).Contains(exportArticle.Link), "TXT tradus păstrează nota și sursa");
Check(File.ReadAllText(translatedRtfExport).Contains("DeepL") && File.ReadAllText(translatedRtfExport).Contains(exportArticle.Link), "RTF tradus păstrează furnizorul și sursa");
Directory.Delete(exportTestFolder, true);
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
var mediafaxLocalOnly = new Feed { Name = "Mediafax -", Url = "http://www.mediafax.ro/rss/economic/" };
var mediafaxRemoteAlias = new NewsBlurSubscription("Mediafax -", "http://feeds.feedburner.com/MediafaxEconomic", "Știri generale");
var mediafaxNameAddressMismatch = NewsBlurFeedSnapshotPolicy.FindNameAddressMismatches([mediafaxLocalOnly], [mediafaxRemoteAlias]);
Check(mediafaxNameAddressMismatch.Count == 1 && ReferenceEquals(mediafaxNameAddressMismatch[0].LocalFeed, mediafaxLocalOnly), "numele identic și URL-ul diferit opresc preventiv oglindirea înaintea importului aliasului");
Check(NewsBlurFeedSnapshotPolicy.FindNameAddressMismatches(
    [mediafaxLocalOnly, new Feed { Name = "Mediafax", Url = "https://other.example.test/rss" }], [mediafaxRemoteAlias]).Count == 0,
    "potrivirea ambiguă a numelui nu atribuie automat un alias");
Check(NewsBlurFeedSnapshotPolicy.FindNameAddressMismatches(
    [new Feed { Name = "Același feed", Url = "https://example.test/rss" }], [new NewsBlurSubscription("Același-feed", "https://www.example.test/rss", "Actualitate")]).Count == 0,
    "numele egal nu avertizează când URL-urile sunt echivalente prin aliasul www acceptat");
var addBefore = new NewsBlurFeedInfo[] { new("7", "Alt feed", "https://other.example.test/rss", "Actualitate") };
var newlyAddedWrongAlias = new NewsBlurFeedInfo("8", "Mediafax -", "http://feeds.feedburner.com/MediafaxEconomic", "Știri generale");
Check(ReferenceEquals(NewsBlurFeedSnapshotPolicy.FindUniqueNewMismatchedFeed(addBefore, [.. addBefore, newlyAddedWrongAlias], mediafaxLocalOnly.Url, mediafaxLocalOnly.Name), newlyAddedWrongAlias),
    "după un POST, aliasul nou cu nume unic poate fi identificat precis pentru revenire");
Check(NewsBlurFeedSnapshotPolicy.FindUniqueNewMismatchedFeed([.. addBefore, newlyAddedWrongAlias], [.. addBefore, newlyAddedWrongAlias], mediafaxLocalOnly.Url, mediafaxLocalOnly.Name) is null,
    "un alias existent înainte de POST nu este declarat nou și nu poate fi șters automat");
Check(NewsBlurFeedSnapshotPolicy.FindUniqueNewMismatchedFeed(addBefore, [.. addBefore, newlyAddedWrongAlias, new NewsBlurFeedInfo("9", "Mediafax -", "https://another.example.test/feed", "Știri")], mediafaxLocalOnly.Url, mediafaxLocalOnly.Name) is null,
    "mai multe abonamente noi cu același nume blochează ștergerea automată ambiguă");
Check(NewsBlurFeedSnapshotPolicy.FindUniqueNewMismatchedFeed(addBefore, [.. addBefore, newlyAddedWrongAlias with { Name = "Mediafax Economic" }], mediafaxLocalOnly.Url, mediafaxLocalOnly.Name) is null,
    "un nume doar asemănător nu este suficient pentru a elimina un feed remote");
Check(NewsBlurFeedSnapshotPolicy.FindUniqueNewMismatchedFeed(addBefore, [.. addBefore, newlyAddedWrongAlias, new NewsBlurFeedInfo("10", "Mediafax -", mediafaxLocalOnly.Url, "Știri")], mediafaxLocalOnly.Url, mediafaxLocalOnly.Name) is null,
    "confirmarea URL-ului exact prevalează și nu șterge niciun feed în plus");
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
Check(NewsBlurReadStatePolicy.Decide(true, false, false, false) == NewsBlurReadSyncAction.SendRead, "marcarea locală ca citit trimite citit către NewsBlur");
Check(NewsBlurReadStatePolicy.Decide(false, true, true, true) == NewsBlurReadSyncAction.SendUnread, "marcarea locală ca necitit trimite necitit către NewsBlur");
Check(NewsBlurReadStatePolicy.Decide(false, false, true, false) == NewsBlurReadSyncAction.ApplyRemote, "schimbarea remote se preia când localul nu s-a schimbat");
Check(NewsBlurReadStatePolicy.Decide(true, false, true, false) == NewsBlurReadSyncAction.AlreadyAligned, "aceeași schimbare pe ambele părți nu este conflict");
Check(NewsBlurReadStatePolicy.Decide(true, false, false, true) == NewsBlurReadSyncAction.Conflict, "schimbările simultane cu valori diferite rămân conflict");
Check(NewsBlurReadStatePolicy.Decide(false, false, false, false) == NewsBlurReadSyncAction.None, "starea neschimbată nu trimite comenzi");
Check(NewsBlurFolderMapping.IsUnorganized(" neorganizate ") && !NewsBlurFolderMapping.IsNamedFolder("Neorganizate"), "Neorganizate este o categorie specială, nu folder NewsBlur propriu-zis");
Check(NewsBlurFolderMapping.ToNewsBlurFolder("Neorganizate") == string.Empty && NewsBlurFolderMapping.FromNewsBlurFolder(null) == "Neorganizate", "feedurile fără folder NewsBlur se mapează la Neorganizate");
var moveToRoot = NewsBlurFolderMapping.CreateMoveFeedParameters("42", "Știri", "Neorganizate");
Check(moveToRoot.Single(item => item.Key == "in_folder").Value == "Știri" && moveToRoot.Single(item => item.Key == "to_folder").Value == string.Empty, "ștergerea folderului mută feedurile la nivelul superior NewsBlur, nu le dezabonează");
var moveFromRoot = NewsBlurFolderMapping.CreateMoveFeedParameters("42", "Neorganizate", "Știri");
Check(moveFromRoot.Single(item => item.Key == "in_folder").Value == string.Empty && moveFromRoot.Single(item => item.Key == "to_folder").Value == "Știri", "mutarea unui feed neorganizat într-un folder folosește nivelul superior ca sursă");

var topicCatalog = CititorRSS.Jaws.Services.Rss.TopicFeedSearchService.GetCatalog();
Check(topicCatalog.Categories.Count > 0, "catalogul de feeduri pe categorii conține categorii definite");
var techResults = CititorRSS.Jaws.Services.Rss.TopicFeedSearchService.SearchCatalog("tehnologie", null);
Check(techResults.Count > 0, "căutarea în catalogul local returnează feeduri pentru 'tehnologie'");
var newsCategoryResults = CititorRSS.Jaws.Services.Rss.TopicFeedSearchService.SearchCatalog(null, "Știri & Actualitate");
Check(newsCategoryResults.Count > 0, "filtrul pe categorie returnează feedurile din Știri & Actualitate");

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

var testFooter = ArticleSharing.BuildFooter("Conținut preluat prin Orizont RSS:", "Sursa articolului:", "https://example.com/stire", "Traducere automată realizată prin Google Translate.");
Check(testFooter.Contains("https://grifnas.github.io/OrizontRSS/"), "footerul de partajare conține linkul Orizont RSS");
Check(testFooter.Contains("Google Translate"), "footerul de partajare menționează serviciul de traducere");
Check(testFooter.Contains("https://example.com/stire"), "footerul de partajare include sursa articolului");
var attributedResponse = ArticleSharing.AppendFooter("Răspunsul AI despre articol", testFooter);
Check(attributedResponse.EndsWith(testFooter, StringComparison.Ordinal), "documentele AI distribuite păstrează footerul complet de atribuire");
Check(attributedResponse.Contains($"{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}Conținut preluat prin Orizont RSS:"), "footerul de atribuire este separat vizibil de răspunsul AI");
Check(ArticleSharing.LimitForUri(attributedResponse, testFooter.Length + 30, testFooter).EndsWith(testFooter, StringComparison.Ordinal), "trunchierea răspunsului AI păstrează footerul complet");
var deepSeekFooter = ArticleSharing.BuildFooter("Conținut preluat prin Orizont RSS:", "Sursa articolului:", "https://example.com/stire", "Traducere automată realizată prin DeepSeek.");
var translatedAiResponse = ArticleSharing.AppendFooter("Traducerea articolului", deepSeekFooter);
Check(translatedAiResponse.Contains("Traducere automată realizată prin DeepSeek.") && translatedAiResponse.Contains("https://example.com/stire"), "distribuirea unei traduceri AI păstrează furnizorul și sursa originală");

var fullShared = ArticleSharing.BuildShareText("Titlu Test", "Textul articolului lung pentru verificare", "https://example.com/stire", "Traducere automată realizată prin Google Translate.", "Conținut preluat prin Orizont RSS:", "Sursa articolului:");
Check(fullShared.StartsWith("Titlu Test") && fullShared.Contains("Textul articolului lung") && fullShared.Contains("https://grifnas.github.io/OrizontRSS/"), "textul complet partajat conține titlul, corpul și footerul cu link");
var contextShared = ArticleSharing.BuildShareText(translatedContext, translatedNote, "Conținut preluat prin Orizont RSS:", "Sursa articolului:");
Check(contextShared.Contains("Translated article body") && contextShared.Contains("Traducere automată realizată prin DeepL în limba română.") && contextShared.Contains(exportArticle.Link), "copierea/partajarea pe baza contextului păstrează traducerea și proveniența");
var translatedFooter = ArticleSharing.BuildFooter(translatedContext, translatedNote, "Conținut preluat prin Orizont RSS:", "Sursa articolului:");
Check(ArticleSharing.LimitForUri(contextShared, translatedFooter.Length + 30, translatedFooter).EndsWith(translatedFooter, StringComparison.Ordinal), "trunchierea distribuției păstrează footerul complet cu furnizor și limbă");

var limitedShared = ArticleSharing.LimitForUri(fullShared, testFooter.Length + 30, testFooter);
Check(limitedShared.EndsWith(testFooter), "limitarea pentru URI protejează întotdeauna footerul");
const string shareBodyWithLineBreaks = "Linia unu\r\nLinia doi\nștire & link=ok";
var mailtoShare = ArticleSharing.CreateMailto("Titlu & știre", shareBodyWithLineBreaks);
var mailtoParts = mailtoShare.Split("&body=", 2, StringSplitOptions.None);
Check(mailtoParts.Length == 2 && mailtoParts[0].StartsWith("mailto:?subject=", StringComparison.Ordinal) &&
      !mailtoParts[0].Contains("Titlu & știre", StringComparison.Ordinal),
    "linkul de e-mail encodează separat subiectul cu diacritice și caractere rezervate URI");
Check(mailtoParts.Length == 2 && Uri.UnescapeDataString(mailtoParts[1]) == "Linia unu\r\nLinia doi\r\nștire & link=ok",
    "corpul mailto poate fi recuperat integral, cu rânduri și diacritice normalizate");
var whatsappShare = ArticleSharing.CreateWhatsApp(shareBodyWithLineBreaks);
var whatsappTextStart = whatsappShare.IndexOf("?text=", StringComparison.Ordinal);
Check(whatsappTextStart >= 0 && Uri.UnescapeDataString(whatsappShare[(whatsappTextStart + 6)..]) == "Linia unu\nLinia doi\nștire & link=ok",
    "linkul WhatsApp păstrează rândurile, diacriticele și ampersandul după decodare");
var longShared = ArticleSharing.BuildShareText("Titlu lung", new string('ș', 6000), "https://example.com/stire",
    "Traducere automată realizată prin DeepL în limba română.", "Conținut preluat prin Orizont RSS:", "Sursa articolului:");
var longFooter = ArticleSharing.BuildFooter("Conținut preluat prin Orizont RSS:", "Sursa articolului:",
    "https://example.com/stire", "Traducere automată realizată prin DeepL în limba română.");
var limitedEmailBody = ArticleSharing.LimitForUri(longShared, ArticleSharing.EmailBodyLimit, longFooter);
Check(limitedEmailBody.Length <= ArticleSharing.EmailBodyLimit && limitedEmailBody.EndsWith(longFooter, StringComparison.Ordinal),
    "corpul e-mailului pentru articol lung respectă limita și păstrează integral furnizorul și sursa");
var limitedWhatsAppBody = ArticleSharing.LimitForUri(longShared, ArticleSharing.WhatsAppBodyLimit, longFooter);
Check(limitedWhatsAppBody.Length <= ArticleSharing.WhatsAppBodyLimit && limitedWhatsAppBody.EndsWith(longFooter, StringComparison.Ordinal),
    "corpul WhatsApp pentru articol lung respectă limita și păstrează integral furnizorul și sursa");

RunRefreshMergeSmoke();
RunNewsBlurConvergenceSmoke();
RunReadableCleanupSmoke();
RunNativePathSmoke();
RunReadabilitySmoke();
await RunParallelRefreshSmokeAsync();
await RunFeedDiscoverySmokeAsync();
await RunUpdaterSmokeAsync();
RunFolderLocalizationSmoke();
Console.WriteLine($"Core smoke test passed: {checks} verificări, inclusiv actualizări sigure, articole, retenție, duplicate, backup și căutare.");

// Actualizarea RSS: articolele ieșite din flux rămân local; ștergerea după vechime aparține regulii de păstrare.
void RunRefreshMergeSmoke()
{
    var day = DateTimeOffset.Now;
    Article Item(string id, int ageDays, bool favorite = false, bool later = false, string? hash = null) =>
        new() { Id = id, Title = id, Content = "vechi", Published = day.AddDays(-ageDays), IsFavorite = favorite, ReadLater = later, NewsBlurStoryHash = hash };

    var favorite = Item("favorit-iesit", 1, favorite: true);
    favorite.Tags = ["important"];
    favorite.AiNotes = [new AiNote { Title = "Rezumat", Content = "notă" }];
    var plain = Item("obisnuit-iesit", 2);
    var stillThere = Item("inca-in-flux", 1);
    stillThere.IsRead = true;
    var fresh = Item("inca-in-flux", 1);
    fresh.Content = "conținut nou";
    var brandNew = Item("nou", 0);

    var merged = ArticleRefreshMerge.Merge([favorite, plain, stillThere], [fresh, brandNew], retentionCutoff: null);
    var byId = merged.Articles.ToDictionary(article => article.Id);
    Check(byId.ContainsKey("favorit-iesit"), "fără regulă de păstrare, favoritul ieșit din fluxul RSS rămâne local");
    Check(byId["favorit-iesit"].Tags.SequenceEqual(["important"]) && byId["favorit-iesit"].AiNotes.Count == 1, "favoritul păstrat își păstrează etichetele și notițele AI");
    Check(byId.ContainsKey("obisnuit-iesit"), "fără regulă de păstrare, articolul obișnuit ieșit din flux rămâne local");
    Check(merged.Articles.Count(article => article.Id == "inca-in-flux") == 1 && ReferenceEquals(byId["inca-in-flux"], fresh), "articolul încă în flux apare o singură dată, cu versiunea descărcată");
    Check(fresh.IsRead && fresh.Content == "conținut nou", "versiunea descărcată preia starea locală și păstrează conținutul nou");
    Check(merged.NewlyReceived == 1 && merged.IgnoredByRetention == 0 && merged.Articles.Count == 4, "numărătoarea articolelor noi nu include articolele păstrate");

    var cutoff = day.AddDays(-30);
    var recentFavorite = Item("favorit-recent", 3, favorite: true);
    var recentPlain = Item("obisnuit-recent", 5);
    var expiredPlain = Item("obisnuit-expirat", 60);
    var expiredLater = Item("mai-tarziu-expirat", 60, later: true);
    var expiredNewsBlur = Item("newsblur-expirat", 60, hash: "hash-1");
    var expiredStillListed = Item("expirat-in-flux", 60);
    var expiredDownloaded = Item("expirat-in-flux", 60);
    var withRetention = ArticleRefreshMerge.Merge(
        [recentFavorite, recentPlain, expiredPlain, expiredLater, expiredNewsBlur, expiredStillListed],
        [expiredDownloaded],
        cutoff);
    var keptIds = withRetention.Articles.Select(article => article.Id).ToHashSet();
    Check(keptIds.Contains("favorit-recent"), "cu regulă de păstrare, favoritul recent ieșit din flux rămâne local");
    Check(keptIds.Contains("obisnuit-recent"), "cu regulă de păstrare, articolul obișnuit din perioada de păstrare rămâne local");
    Check(!keptIds.Contains("obisnuit-expirat"), "articolul obișnuit expirat și ieșit din flux nu este readus");
    Check(keptIds.Contains("mai-tarziu-expirat"), "articolul Mai târziu expirat rămâne protejat");
    Check(keptIds.Contains("newsblur-expirat"), "articolul NewsBlur rămâne pentru sincronizarea pe mai multe calculatoare");
    Check(!keptIds.Contains("expirat-in-flux") && withRetention.IgnoredByRetention == 1, "articolul expirat încă publicat în flux este ignorat și numărat ca atare");
}

// Sincronizarea NewsBlur: aceeași schimbare pe ambele părți actualizează bazele, fără conflict și fără comenzi.
void RunNewsBlurConvergenceSmoke()
{
    static Article Synced(bool read = false, bool favorite = false, List<string>? tags = null) => new()
    {
        Id = "nb", IsRead = read, IsFavorite = favorite, Tags = tags ?? [],
        NewsBlurStoryHash = "hash", NewsBlurLastRead = read, NewsBlurLastLocalRead = read,
        NewsBlurLastStarred = favorite, NewsBlurLastLocalStarred = favorite, NewsBlurLastLocalSaved = favorite,
        NewsBlurLastTags = (tags ?? []).ToList(), NewsBlurLastLocalTags = (tags ?? []).ToList()
    };
    static NewsBlurStory Story(bool read, bool starred = false, List<string>? tags = null) =>
        new("hash", "Titlu", "https://example.test/a", null, read, starred, tags ?? [], string.Empty);
    static bool Saved(Article article) => article.IsFavorite;
    static void ApplySaved(Article article, bool saved) => article.IsFavorite = saved;

    // Citit pe telefon și pe calculator.
    var article = Synced();
    article.IsRead = true;
    var result = NewsBlurArticleStateReconciler.Reconcile(article, Story(read: true), Saved, ApplySaved);
    Check(result.ReadAction == NewsBlurReadSyncAction.AlreadyAligned && result.Conflicts == 0, "citit pe ambele părți nu raportează conflict");
    Check(article.NewsBlurLastRead == true && article.NewsBlurLastLocalRead == true, "citit pe ambele părți actualizează ambele baze");
    // Apoi necitit pe telefon: se preia, nu se retrimite citit.
    result = NewsBlurArticleStateReconciler.Reconcile(article, Story(read: false), Saved, ApplySaved);
    Check(result.ReadAction == NewsBlurReadSyncAction.ApplyRemote && !article.IsRead, "necititul ulterior de pe telefon este preluat local, nu anulat");

    // Favorit pe ambele părți.
    article = Synced();
    article.IsFavorite = true;
    result = NewsBlurArticleStateReconciler.Reconcile(article, Story(read: false, starred: true), Saved, ApplySaved);
    Check(result.Conflicts == 0 && !result.SendStar && !result.SendUnstar, "favoritul pe ambele părți nu raportează conflict și nu trimite comenzi");
    Check(article.NewsBlurLastStarred == true && article.NewsBlurLastLocalSaved == true, "favoritul pe ambele părți actualizează bazele");

    // Aceleași etichete pe ambele părți, cu diferență de majuscule.
    article = Synced(favorite: true);
    article.Tags = ["Tehnologie"];
    result = NewsBlurArticleStateReconciler.Reconcile(article, Story(read: false, starred: true, tags: ["tehnologie"]), Saved, ApplySaved);
    Check(result.Conflicts == 0 && !result.SendStarForTags, "aceleași etichete pe ambele părți nu raportează conflict");
    Check(NewsBlurArticleStateReconciler.SameTags(article.NewsBlurLastLocalTags, ["Tehnologie"]) && NewsBlurArticleStateReconciler.SameTags(article.NewsBlurLastTags, ["tehnologie"]), "etichetele convergente actualizează bazele");

    // Conflictele reale rămân neschimbate.
    article = Synced(favorite: true);
    article.Tags = ["local"];
    result = NewsBlurArticleStateReconciler.Reconcile(article, Story(read: false, starred: true, tags: ["remote"]), Saved, ApplySaved);
    Check(result.Conflicts == 1 && article.NewsBlurLastLocalTags!.Count == 0, "etichetele diferite pe ambele părți rămân conflict, cu bazele neschimbate");

    // Comportamente existente păstrate.
    article = Synced();
    article.IsRead = true;
    result = NewsBlurArticleStateReconciler.Reconcile(article, Story(read: false), Saved, ApplySaved);
    Check(result.ReadAction == NewsBlurReadSyncAction.SendRead && article.NewsBlurLastLocalRead == false, "citirea locală se trimite, iar baza se actualizează abia după confirmarea NewsBlur");
    article = Synced();
    result = NewsBlurArticleStateReconciler.Reconcile(article, Story(read: false, starred: true), Saved, ApplySaved);
    Check(article.IsFavorite && result.AppliedRemote == 1, "favoritul adăugat în NewsBlur este preluat local");
    article = Synced();
    article.Tags = ["nou"];
    result = NewsBlurArticleStateReconciler.Reconcile(article, Story(read: false), Saved, ApplySaved);
    Check(result.SkippedTags && !result.SendStarForTags, "etichetele unui articol nesalvat rămân amânate");
}

// Găsirea feedurilor: numai feeduri verificate, reguli pentru platforme, pagini-listă, adrese uzuale ca rezervă.
async Task RunFeedDiscoverySmokeAsync()
{
    static string Rss(string title, string firstId) =>
        $"<rss><channel><title>{title}</title><item><guid>{firstId}</guid><title>Articol</title><link>https://x.test/{firstId}</link><pubDate>Mon, 13 Oct 2026 08:00:00 GMT</pubDate></item><item><guid>{firstId}-2</guid><title>Al doilea</title></item></channel></rss>";
    static string Html(string head, string body = "") => $"<html><head>{head}</head><body>{body}</body></html>";
    var pages = new Dictionary<string, (string Type, string Body)>(StringComparer.OrdinalIgnoreCase)
    {
        ["https://stiri.test/"] = ("text/html", Html("<link rel=\"alternate\" type=\"application/rss+xml\" href=\"/feed\"><link rel=\"alternate\" type=\"application/rss+xml\" href=\"/comments/feed\">", "<a href=\"/feed/\">RSS</a><a href=\"/rss-mort\">rss</a>")),
        ["https://stiri.test/feed"] = ("application/rss+xml", Rss("Știri Test", "s1")),
        ["https://stiri.test/feed/"] = ("application/rss+xml", Rss("Știri Test", "s1")),
        ["https://stiri.test/comments/feed"] = ("application/rss+xml", Rss("Comentarii la Știri Test", "c1")),
        ["https://stiri.test/2026/10/articol"] = ("text/html", Html("<title>Articol</title>")),
        ["https://www.youtube.com/@canal"] = ("text/html", Html("", "<script>var d = {\"channelId\":\"UC1234567890123456789012\"};</script>")),
        ["https://www.youtube.com/feeds/videos.xml?channel_id=UC1234567890123456789012"] = ("application/atom+xml", "<feed xmlns=\"http://www.w3.org/2005/Atom\"><title>Canal</title><entry><id>v1</id><title>Video</title></entry></feed>"),
        ["https://www.reddit.com/r/Romania/"] = ("text/html", Html("<title>r/Romania</title>")),
        ["https://www.reddit.com/r/Romania/.rss"] = ("application/atom+xml", "<feed xmlns=\"http://www.w3.org/2005/Atom\"><title>r/Romania</title><entry><id>t1</id><title>Post</title></entry></feed>"),
        ["https://revista.test/"] = ("text/html", Html("", "<footer><a href=\"/despre-rss\">Fluxuri RSS</a></footer>")),
        ["https://revista.test/despre-rss"] = ("text/html", Html("", "<a href=\"https://revista.test/export/rss-stiri.xml\">Știri</a>")),
        ["https://revista.test/export/rss-stiri.xml"] = ("application/xml", Rss("Revista", "r1")),
        ["https://simplu.test/"] = ("text/html", Html("<title>Simplu</title>")),
        ["https://simplu.test/rss.xml"] = ("application/rss+xml", Rss("Simplu", "p1")),
    };
    var requested = new List<string>();
    var handler = new UpdateSmokeHttpHandler(request =>
    {
        var url = request.RequestUri!.ToString();
        lock (requested) requested.Add(url);
        if (!pages.TryGetValue(url, out var page)) return new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request };
        return new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request, Content = new StringContent(page.Body, Encoding.UTF8, page.Type) };
    });
    var discovery = new FeedDiscoveryService(handler);

    var site = await discovery.DiscoverAsync("stiri.test");
    Check(site.Count == 2 && site[0].Url == "https://stiri.test/feed" && site[0].Name == "Știri Test" && site[0].ArticleCount == 2 && site[0].LatestArticle is not null, "feedul declarat apare primul, verificat, cu titlu, număr de articole și data ultimului articol");
    Check(site.All(feed => feed.IsVerified) && !site.Any(feed => feed.Url.Contains("rss-mort", StringComparison.Ordinal)), "în rezultat apar numai feedurile care funcționează");
    Check(site[1].IsComments && site[1].Url.EndsWith("/comments/feed", StringComparison.Ordinal), "feedul de comentarii este marcat și pus la final");
    Check(!site.Any(feed => feed.Url == "https://stiri.test/feed/"), "aceeași adresă cu bară finală nu apare de două ori");
    Check((await discovery.DiscoverAsync("https://stiri.test/feed")).Single().Url == "https://stiri.test/feed", "o adresă care este deja feed este recunoscută direct");
    Check((await discovery.DiscoverAsync("https://stiri.test/2026/10/articol")).Any(feed => feed.Url == "https://stiri.test/feed"), "adresa unui articol duce la feedul declarat pe pagina principală");
    Check((await discovery.DiscoverAsync("https://www.youtube.com/@canal")).Single().Url.EndsWith("channel_id=UC1234567890123456789012", StringComparison.Ordinal), "canalul YouTube produce feedul canalului");
    Check((await discovery.DiscoverAsync("https://www.reddit.com/r/Romania/")).Single().Url == "https://www.reddit.com/r/Romania/.rss", "subredditul produce feedul lui");
    Check((await discovery.DiscoverAsync("revista.test")).Single().Url == "https://revista.test/export/rss-stiri.xml", "pagina-listă de feeduri a site-ului este citită");
    Check((await discovery.DiscoverAsync("simplu.test")).Single().Url == "https://simplu.test/rss.xml", "adresele uzuale sunt încercate ca rezervă; numai cea funcțională apare în rezultat");
    Check(requested.Contains("https://simplu.test/feed"), "adresele uzuale nefuncționale au fost încercate, dar nu sunt afișate");
    var invalid = false;
    try { await discovery.DiscoverAsync("nu este adresa"); } catch (InvalidOperationException) { invalid = true; }
    Check(invalid, "o adresă nevalidă este respinsă cu un mesaj clar");

    var allCategories = CititorRSS.Jaws.Services.Rss.TopicFeedSearchService.SearchCatalog(null, null).Count;
    var technology = CititorRSS.Jaws.Services.Rss.TopicFeedSearchService.SearchCatalog(null, "Tehnologie & IT");
    Check(allCategories > 0 && technology.Count > 0 && technology.Count < allCategories && technology.All(feed => feed.Category == "Tehnologie & IT"), "filtrul de categorii folosește cheia categoriei, independent de limba interfeței");
}

// Actualizare rapidă: descărcări paralele limitate și răspunsul „nimic nou” (HTTP 304).
async Task RunParallelRefreshSmokeAsync()
{
    var empty = new RssLoadResult(false, [], null, null);
    var feeds = Enumerable.Range(0, 12).Select(i => new Feed { Name = $"F{i}", Url = $"https://site{i % 3}.example.test/rss{i}" }).ToList();
    int running = 0, maxRunning = 0;
    var perHost = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
    var maxPerHost = 0;
    var watch = System.Diagnostics.Stopwatch.StartNew();
    var downloads = FeedDownloadScheduler.Start(feeds, async (feed, token) =>
    {
        var host = new Uri(feed.Url).Host;
        var now = Interlocked.Increment(ref running);
        var hostNow = perHost.AddOrUpdate(host, 1, (_, value) => value + 1);
        lock (perHost) { maxRunning = Math.Max(maxRunning, now); maxPerHost = Math.Max(maxPerHost, hostNow); }
        await Task.Delay(150, token);
        perHost.AddOrUpdate(host, 0, (_, value) => value - 1);
        Interlocked.Decrement(ref running);
        return empty;
    }, CancellationToken.None);
    var results = await Task.WhenAll(downloads);
    watch.Stop();
    Check(results.All(result => result.Error is null && result.Result is not null), "toate descărcările paralele se termină");
    Check(maxPerHost <= FeedDownloadScheduler.MaximumPerHost && maxRunning <= FeedDownloadScheduler.MaximumParallel, "descărcările respectă limita generală și limita pe site");
    Check(watch.ElapsedMilliseconds < 12 * 150, "descărcările paralele sunt mai rapide decât cele succesive");

    var order = new List<string>();
    var mixed = new List<Feed> { new() { Name = "lent", Url = "https://lent.example.test/rss" }, new() { Name = "rapid1", Url = "https://a.example.test/rss" }, new() { Name = "rapid2", Url = "https://b.example.test/rss" } };
    var mixedDownloads = FeedDownloadScheduler.Start(mixed, async (feed, token) =>
    {
        await Task.Delay(feed.Name == "lent" ? 600 : 30, token);
        if (feed.Name == "rapid2") throw new HttpRequestException("server indisponibil");
        return empty;
    }, CancellationToken.None);
    while (mixedDownloads.Count > 0)
    {
        var done = await Task.WhenAny(mixedDownloads);
        mixedDownloads.Remove(done);
        order.Add((await done).Feed.Name);
    }
    Check(order.Last() == "lent", "un feed lent nu blochează prelucrarea celorlalte");
    var failedDownload = (await FeedDownloadScheduler.Start([mixed[2]], async (_, _) => { await Task.Yield(); throw new HttpRequestException("x"); }, CancellationToken.None)[0]);
    Check(failedDownload.Error?.SourceException is HttpRequestException, "eroarea unei descărcări este păstrată pentru tratarea obișnuită");
    using var cancelled = new CancellationTokenSource();
    cancelled.Cancel();
    var cancelledDownload = await FeedDownloadScheduler.Start([mixed[1]], (_, _) => Task.FromResult(empty), cancelled.Token)[0];
    Check(cancelledDownload.Error?.SourceException is OperationCanceledException, "anularea oprește descărcările care nu au început");

    const string feedXml = "<rss><channel><item><guid>a1</guid><title>Primul</title><link>https://n.example.test/a1</link><description>Text</description></item></channel></rss>";
    var conditionalHeaders = new List<string>();
    var reader = new RssReader(new UpdateSmokeHttpHandler(request =>
    {
        conditionalHeaders.Add($"{request.Headers.IfNoneMatch}|{request.Headers.IfModifiedSince?.ToString("R")}");
        if (request.Headers.IfNoneMatch.Count > 0) return new HttpResponseMessage(HttpStatusCode.NotModified);
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(feedXml, Encoding.UTF8, "application/rss+xml") };
        response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"v1\"");
        response.Content.Headers.LastModified = new DateTimeOffset(2026, 10, 13, 8, 0, 0, TimeSpan.Zero);
        return response;
    }));
    var first = await reader.LoadIfChangedAsync("https://n.example.test/rss", null, null);
    Check(!first.NotModified && first.Articles.Count == 1 && first.ETag == "\"v1\"" && first.LastModified is not null, "prima descărcare citește articolele și reține versiunea serverului");
    Check(conditionalHeaders[0] == "|", "fără versiune reținută nu se trimit antete condiționate");
    var second = await reader.LoadIfChangedAsync("https://n.example.test/rss", first.ETag, first.LastModified);
    Check(second.NotModified && second.Articles.Count == 0 && second.ETag == first.ETag, "la „nimic nou” (304) fluxul nu este descărcat din nou");
    Check(conditionalHeaders[1].StartsWith("\"v1\"|", StringComparison.Ordinal) && conditionalHeaders[1].EndsWith("GMT", StringComparison.Ordinal), "versiunea reținută este trimisă serverului");
    Check((await reader.LoadAsync("https://n.example.test/rss")).Count == 1, "descărcarea obișnuită rămâne neschimbată");
}

// Textul integral: SmartReader alege articolul; paginile nerecunoscute folosesc extractorul existent.
void RunReadabilitySmoke()
{
    string[] paragraphs =
    [
        "Primăria a anunțat marți că lucrările de reabilitare a podului vor începe în noiembrie, după finalizarea licitației, iar traficul va fi deviat pe străzile paralele timp de trei luni.",
        "Potrivit proiectului, structura metalică va fi consolidată, iar trotuarele vor fi lărgite pentru a permite accesul persoanelor cu dizabilități, inclusiv al celor care folosesc fotolii rulante.",
        "Constructorul trebuie să respecte un calendar strict, cu penalități pentru fiecare zi de întârziere, a explicat directorul tehnic, care a adăugat că inspecțiile vor fi săptămânale.",
        "Locuitorii din zonă au cerut informații clare despre rutele ocolitoare și despre programul transportului public; formula afișată în stații va fi: timp < 30 de minute & frecvență mare."
    ];
    var page = "<html><head><title>Podul va fi reabilitat</title></head><body>" +
        "<nav><a href=\"/\">Acasă</a> <a href=\"/sport\">Sport</a> <a href=\"/meteo\">Meteo</a></nav>" +
        "<article><h1>Podul va fi reabilitat</h1>" + string.Concat(paragraphs.Select(p => $"<p>{System.Net.WebUtility.HtmlEncode(p)}</p>")) + "</article>" +
        "<aside><h3>Cele mai citite</h3><ul><li><a href=\"/a\">Alt titlu</a></li><li><a href=\"/b\">Încă un titlu</a></li></ul></aside>" +
        "<footer>Toate drepturile rezervate. Politica de cookie-uri.</footer></body></html>";
    var text = RssReader.ExtractReadableArticle("https://stiri.example.test/pod", page);
    Check(paragraphs.All(p => text.Contains(p, StringComparison.Ordinal)), "articolul extras conține toate paragrafele, inclusiv caracterele < și &");
    Check(!text.Contains("Meteo", StringComparison.Ordinal) && !text.Contains("Cele mai citite", StringComparison.Ordinal) && !text.Contains("drepturile rezervate", StringComparison.Ordinal), "meniul, lista laterală și subsolul nu intră în textul articolului");
    Check(text.Split('\n').Count(line => line.Trim().Length > 0) >= paragraphs.Length, "paragrafele rămân pe linii separate pentru navigarea cu cititorul de ecran");

    var linkList = "<html><body><ul>" + string.Concat(Enumerable.Range(1, 8).Select(i => $"<li><a href=\"/s{i}\">Știre {i}</a></li>")) + "</ul></body></html>";
    Check(RssReader.ExtractReadableArticle("https://stiri.example.test/", linkList) == RssReader.CleanReadableContent(linkList), "o pagină care nu este articol folosește extractorul existent");
    Check(RssReader.ExtractReadableArticle("https://stiri.example.test/", string.Empty) == string.Empty, "pagina goală produce text gol");
}

// eSpeak NG primește calea ca șir de 8 biți: rezolvarea nu întoarce niciodată o cale cu diacritice.
void RunNativePathSmoke()
{
    Check(EspeakVolume.FromSetting(100) == 200 && EspeakVolume.FromSetting(50) == 100 && EspeakVolume.FromSetting(0) == 0, "volumul aplicației acoperă întreaga gamă eSpeak NG (0–200)");
    Check(EspeakVolume.FromSetting(150) == 200 && EspeakVolume.FromSetting(-5) == 0, "volumul eSpeak NG rămâne în limitele motorului");
    Check(NativePath.ForAnsiNativeApi(@"C:\Program Files\Orizont RSS\SpeechEngines\eSpeakNG") == @"C:\Program Files\Orizont RSS\SpeechEngines\eSpeakNG", "calea ASCII este transmisă neschimbată");
    Check(NativePath.ForAnsiNativeApi(Path.Combine(Path.GetTempPath(), $"lipsește-{Guid.NewGuid():N}-ș")) is null, "o cale inexistentă cu diacritice nu este transmisă motorului");
    var folder = Path.Combine(Path.GetTempPath(), $"Orizont-test-Frișan-ț-{Guid.NewGuid():N}");
    Directory.CreateDirectory(folder);
    try
    {
        var resolved = NativePath.ForAnsiNativeApi(folder);
        Check(resolved is null || (NativePath.IsAscii(resolved) && Directory.Exists(resolved)), "calea cu ș/ț devine nume scurt ASCII existent sau este refuzată");
    }
    finally
    {
        Directory.Delete(folder);
    }
}

// Localizarea poziției și a folderului implicit; exportul OPML pune feedurile neorganizate la rădăcină.
// Rulează ultima, deoarece schimbă limba interfeței.
void RunFolderLocalizationSmoke()
{
    foreach (var culture in new[] { "en-US", "es-ES", "fr-FR", "de-DE", "pt-BR", "hu-HU", "it-IT" })
    {
        UiCulture.Apply(culture);
        Check(UiText.Translate("Toate folderele").Length > 3 && UiText.Translate("Neorganizate") != "Neorganizate", $"„Toate folderele” și „Neorganizate” sunt traduse complet în {culture}");
    }

    UiCulture.Apply("en-US");
    var positioned = new Article { Title = "Test", DisplayIndex = 3, DisplayTotal = 50 };
    Check(positioned.DisplayName.Contains("3 of 50", StringComparison.Ordinal) && !positioned.DisplayName.Contains(" din ", StringComparison.Ordinal), "poziția articolului se citește în limba interfeței");
    var unorganized = new Feed { Name = "Demo" };
    Check(unorganized.DisplayName.Contains("folder: Unorganized", StringComparison.Ordinal) && unorganized.VisualDetails.Contains("Unorganized", StringComparison.Ordinal), "folderul implicit este afișat tradus");
    Check(NewsBlurFolderMapping.DisplayName("Neorganizate") == "Unorganized" && NewsBlurFolderMapping.DisplayName(null) == "Unorganized" && NewsBlurFolderMapping.DisplayName("Tehnologie") == "Tehnologie", "numai folderul implicit este tradus la afișare");
    Check(unorganized.Folder == "Neorganizate" && NewsBlurFolderMapping.IsUnorganized(unorganized.Folder), "cheia internă a folderului implicit rămâne neschimbată");
    UiCulture.Apply("ro-RO");
    Check(new Feed { Name = "Demo" }.DisplayName.Contains("folder: Neorganizate", StringComparison.Ordinal), "în română folderul implicit rămâne Neorganizate");

    var body = OpmlExport.CreateBody([
        new Feed { Name = "B", Url = "https://b.example.test/rss", Folder = "Tehnologie" },
        new Feed { Name = "A", Url = "https://a.example.test/rss", Folder = "Neorganizate" },
        new Feed { Name = "C", Url = "https://c.example.test/rss", Folder = "tehnologie" }]);
    var groups = body.Elements("outline").Where(outline => outline.Attribute("xmlUrl") is null).ToList();
    var rootFeeds = body.Elements("outline").Where(outline => outline.Attribute("xmlUrl") is not null).ToList();
    Check(groups.Count == 1 && groups[0].Elements("outline").Count() == 2, "exportul OPML grupează feedurile pe foldere numite");
    Check(rootFeeds.Count == 1 && rootFeeds[0].Attribute("xmlUrl")!.Value == "https://a.example.test/rss", "feedurile neorganizate sunt exportate la rădăcina OPML");
    Check(!body.Descendants("outline").Any(outline => (string?)outline.Attribute("text") == "Neorganizate"), "exportul OPML nu mai creează folderul „Neorganizate”");
}

// Curățarea textului lizibil: elimină marcajele paginii, dar nu cuvintele obișnuite din articol.
void RunReadableCleanupSmoke()
{
    static string Clean(string html) => RssReader.CleanReadableContent($"<html><body><article>{html}</article></body></html>");
    static string Words(string text) => System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

    // Marcaje de pagină care trebuie eliminate în continuare.
    foreach (var label in new[] { "Publicitate", "Advertisement", "Werbung", "Publicidad", "Publicité", "Publicidade", "Hirdetés", "Pubblicità" })
        Check(Words(Clean($"<p>Paragraf unu.</p><div class=\"ad\">{label}</div><p>Paragraf doi.</p>")) == "Paragraf unu. Paragraf doi.", $"eticheta de reclamă „{label}” pe linie proprie este eliminată");
    Check(Words(Clean("<p>Text înainte <span>PUBLICITATE</span> text după.</p>")) == "Text înainte text după.", "marcajul de reclamă cu majuscule din interiorul rândului este eliminat");
    foreach (var label in new[] { "Postați comentariul dvs.", "Leave a comment", "Kommentare", "Commentaires", "Hozzászólás írása" })
        Check(Words(Clean($"<p>Corp.</p><div>{label}</div><p>Final.</p>")) == "Corp. Final.", $"eticheta de comentarii „{label}” pe linie proprie este eliminată");
    Check(Words(Clean("<p>Corp.</p><a>Comments (12)</a>")) == "Corp.", "numărul de comentarii este eliminat");
    Check(Words(Clean("<p>Corp articol.</p><h2>Related articles</h2><ul><li>Alt articol</li></ul>")) == "Corp articol.", "blocul de articole înrudite de la final este tăiat");
    Check(Words(Clean("<p>Corp articol.</p><span>x</span><h2>Reader comments</h2><p>Primul comentariu</p>")) == "Corp articol. x", "comentariile cititorilor sunt tăiate și după un element inline");
    Check(Words(Clean("<p>Corp articol.</p><div id=\"opinions-total\">Total reader comments: <b>2</b></div><p>Recommended</p>")) == "Corp articol.", "antetul GSMArena „Total reader comments: N” taie comentariile și recomandările");
    Check(Words(Clean("<p>Corp articol.</p><p>These are the best offers from our affiliate partners</p><p>Ofertă</p>")) == "Corp articol.", "oferta afiliată de la final este tăiată");
    Check(Words(Clean("<p>Corp articol.</p><p>Add as a preferred source on Google</p><p>Meniu</p>")) == "Corp articol.", "îndemnul de sursă preferată Google este tăiat");
    Check(Words(Clean("<p>Corp articol.</p><div>O campanie editorială Exemplu</div><p>Politici</p>")) == "Corp articol.", "subsolul „O campanie editorială” este tăiat");
    Check(Words(Clean("<p>Corp articol.</p><div>1 / 5</div><p>Final.</p>")) == "Corp articol. Final.", "contorul de galerie este eliminat");
    Check(!Clean("<p>Corp.</p><p>googletag.cmd.push(function() { googletag.display('ad'); });</p>").Contains("googletag", StringComparison.Ordinal), "codul de reclamă rămas ca text este eliminat");

    // Fraze obișnuite care trebuie păstrate întocmai.
    foreach (var sentence in new[]
    {
        "La industria de la publicidad creció un 5% este año.",
        "Il ministro ha rifiutato ogni commento sulla vicenda.",
        "Der Kommentar des Ministers war deutlich.",
        "Laissez votre commentaire sur le site officiel.",
        "O comentário do presidente foi longo.",
        "A megjegyzés fontos volt.",
        "The advertising market grew this year.",
        "Primul paragraf. Articole înrudite cu tema au mai apărut. Al doilea paragraf important.",
        "Comentariile cititorilor au fost moderate ieri, iar discuția continuă.",
        "A fost doar o campanie editorială reușită, spune autorul, și urmează alta."
    })
        Check(Words(Clean($"<p>{sentence}</p>")) == sentence, $"fraza obișnuită rămâne intactă: {sentence}");
}

async Task RunUpdaterSmokeAsync()
{
    const string apiUrl = "https://api.github.test/repos/grifnas/OrizontRSS/releases/latest";
    const string version = "2.0.0";
    var installerName = $"OrizontSetup-{version}.exe";
    var installerUrl = $"https://github.com/grifnas/OrizontRSS/releases/download/v{version}/{installerName}";
    var checksumUrl = installerUrl + ".sha256";
    var releaseJson = BuildReleaseJson(version,
        ($"{installerName}.sha256", checksumUrl),
        ($"Orizont-RSS-{version}-win-x64.zip", $"https://github.com/grifnas/OrizontRSS/releases/download/v{version}/portable.zip"),
        (installerName, installerUrl));

    using (var client = CreateUpdateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releaseJson) }))
    {
        var update = await UpdateCheckerService.CheckForUpdatesAsync(client, apiUrl, "1.6.0");
        Check(update.Status == UpdateCheckStatus.UpdateAvailable && update.DownloadUrl == installerUrl && update.ChecksumDownloadUrl == checksumUrl,
            "updater selects exact installer and matching checksum when sidecar is listed first");
    }

    var orphanChecksum = BuildReleaseJson(version, ($"{installerName}.sha256", checksumUrl),
        ("portable.zip", "https://github.com/grifnas/OrizontRSS/releases/download/v2.0.0/portable.zip"));
    using (var client = CreateUpdateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(orphanChecksum) }))
    {
        var update = await UpdateCheckerService.CheckForUpdatesAsync(client, apiUrl, "1.6.0");
        Check(update.Status == UpdateCheckStatus.Error && !update.HasUpdate, "missing installer is an explicit error, never an HTML download");
    }

    using (var client = CreateUpdateClient(_ => new HttpResponseMessage(HttpStatusCode.Forbidden) { ReasonPhrase = "Forbidden" }))
    {
        var update = await UpdateCheckerService.CheckForUpdatesAsync(client, apiUrl, "1.6.0");
        Check(update.Status == UpdateCheckStatus.Error && update.ErrorMessage.Contains("403", StringComparison.Ordinal),
            "HTTP check error is distinct from up to date");
    }

    using (var client = CreateUpdateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(BuildReleaseJson("1.6.0")) }))
    {
        var update = await UpdateCheckerService.CheckForUpdatesAsync(client, apiUrl, "1.6.0");
        Check(update.Status == UpdateCheckStatus.UpToDate && !update.HasUpdate, "successful version comparison can report up to date");
    }

    var missingTagRelease = JsonSerializer.Serialize(new { tag_name = "", html_url = "https://github.com/grifnas/OrizontRSS/releases/tag/unknown" });
    using (var client = CreateUpdateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(missingTagRelease) }))
    {
        var update = await UpdateCheckerService.CheckForUpdatesAsync(client, apiUrl, "1.6.0");
        Check(update.Status == UpdateCheckStatus.Error && !update.HasUpdate, "missing release version tag is not treated as up to date");
    }

    var peBytes = MakeSyntheticPe();
    var expectedHash = Convert.ToHexString(SHA256.HashData(peBytes));
    var updateInfo = new AppUpdateInfo { Status = UpdateCheckStatus.UpdateAvailable, DownloadUrl = installerUrl, ChecksumDownloadUrl = checksumUrl };
    var tempRoot = Path.Combine(Path.GetTempPath(), $"OrizontUpdateSmoke-{Guid.NewGuid():N}");
    Directory.CreateDirectory(tempRoot);
    try
    {
        using (var client = CreateUpdateClient(request => request.RequestUri!.AbsolutePath.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($"{expectedHash}  {installerName}\n") }
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(peBytes) }))
        {
            var prepared = await UpdateCheckerService.DownloadAndVerifyAsync(updateInfo, client, tempRoot);
            var preparedDirectory = Path.GetDirectoryName(prepared.InstallerPath)!;
            Check(File.Exists(prepared.InstallerPath) && (await File.ReadAllBytesAsync(prepared.InstallerPath)).SequenceEqual(peBytes),
                "synthetic installer passes SHA-256 and PE header checks");
            prepared.Dispose();
            Check(!Directory.Exists(preparedDirectory), "verified installer temp files are cleaned on disposal");
        }

        using (var client = CreateUpdateClient(request => request.RequestUri!.AbsolutePath.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(new string('0', 64)) }
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(peBytes) }))
        {
            var rejected = await ThrowsUpdateAsync<InvalidDataException>(() => UpdateCheckerService.DownloadAndVerifyAsync(updateInfo, client, tempRoot));
            Check(rejected && !Directory.EnumerateDirectories(tempRoot).Any(), "checksum mismatch is rejected and cleaned up");
        }

        using (var client = CreateUpdateClient(request => request.RequestUri!.AbsolutePath.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(expectedHash) }
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = CreateIncompleteContent(peBytes) }))
        {
            var rejected = await ThrowsUpdateAsync<InvalidDataException>(() => UpdateCheckerService.DownloadAndVerifyAsync(updateInfo, client, tempRoot));
            Check(rejected && !Directory.EnumerateDirectories(tempRoot).Any(), "incomplete installer download is rejected and cleaned up");
        }

        using (var client = CreateUpdateClient(request => request.RequestUri!.AbsolutePath.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(expectedHash) }
            : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)))
        {
            var rejected = await ThrowsUpdateAsync<HttpRequestException>(() => UpdateCheckerService.DownloadAndVerifyAsync(updateInfo, client, tempRoot));
            Check(rejected && !Directory.EnumerateDirectories(tempRoot).Any(), "installer HTTP error prevents staging");
        }
    }
    finally
    {
        if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
    }

    var order = new List<string>();
    var didClose = await SafeUpdateExitFlow.SaveThenStartAndCloseAsync(
        () => { order.Add("save"); return Task.CompletedTask; },
        () => { order.Add("start"); return true; },
        () => order.Add("close"));
    Check(didClose && order.SequenceEqual(new[] { "save", "start", "close" }), "updater saves, starts installer, then closes");

    var launchAfterSaveFailure = false;
    var closeAfterSaveFailure = false;
    var saveFailed = await ThrowsUpdateAsync<InvalidOperationException>(() => SafeUpdateExitFlow.SaveThenStartAndCloseAsync(
        () => Task.FromException(new InvalidOperationException("simulated save failure")),
        () => { launchAfterSaveFailure = true; return true; },
        () => closeAfterSaveFailure = true));
    Check(saveFailed && !launchAfterSaveFailure && !closeAfterSaveFailure, "save failure prevents update launch and app close");

    var closedAfterStartFailure = false;
    var started = await SafeUpdateExitFlow.SaveThenStartAndCloseAsync(() => Task.CompletedTask, () => false, () => closedAfterStartFailure = true);
    Check(!started && !closedAfterStartFailure, "failed installer launch keeps app open");
}

static string BuildReleaseJson(string version, params (string Name, string Url)[] assets) => JsonSerializer.Serialize(new
{
    tag_name = $"v{version}",
    html_url = $"https://github.com/grifnas/OrizontRSS/releases/tag/v{version}",
    body = "Synthetic release notes",
    assets = assets.Select(asset => new { name = asset.Name, browser_download_url = asset.Url })
});

static byte[] MakeSyntheticPe()
{
    var bytes = new byte[128];
    bytes[0] = (byte)'M'; bytes[1] = (byte)'Z';
    BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x3c, 4), 64);
    bytes[64] = (byte)'P'; bytes[65] = (byte)'E';
    return bytes;
}

static HttpClient CreateUpdateClient(Func<HttpRequestMessage, HttpResponseMessage> responder) => new(new UpdateSmokeHttpHandler(responder));

static HttpContent CreateIncompleteContent(byte[] bytes)
{
    var content = new ByteArrayContent(bytes);
    content.Headers.ContentLength = bytes.Length + 7;
    return content;
}

static async Task<bool> ThrowsUpdateAsync<TException>(Func<Task> action) where TException : Exception
{
    try { await action(); return false; }
    catch (TException) { return true; }
}

sealed class UpdateSmokeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(responder(request));
}
