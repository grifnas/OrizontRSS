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
Check(NewsBlurReadStatePolicy.Decide(true, false, true, false) == NewsBlurReadSyncAction.Conflict, "schimbările simultane păstrează politica de conflict existentă");
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

await RunUpdaterSmokeAsync();
Console.WriteLine($"Core smoke test passed: {checks} verificări, inclusiv actualizări sigure, articole, retenție, duplicate, backup și căutare.");

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
