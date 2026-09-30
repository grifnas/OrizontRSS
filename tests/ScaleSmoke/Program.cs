using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CititorRSS.Jaws;
using CititorRSS.Jaws.Services.Content;

const int feedCount = 100;
const int articlesPerFeed = 500;
const int expectedArticles = feedCount * articlesPerFeed;
var timer = Stopwatch.StartNew();
var checks = 0;

void Check(bool condition, string description)
{
    checks++;
    if (!condition) throw new InvalidOperationException($"Eșec: {description}");
}

var now = DateTimeOffset.UtcNow;
var feeds = Enumerable.Range(0, feedCount)
    .Select(index => new Feed
    {
        Name = $"Feed {index:000}",
        Url = $"https://scale.test/feed/{index:000}",
        Folder = index % 2 == 0 ? "Tehnologie" : "Actualitate"
    })
    .ToList();

foreach (var (feed, feedIndex) in feeds.Select((feed, index) => (feed, index)))
{
    for (var articleIndex = 0; articleIndex < articlesPerFeed; articleIndex++)
    {
        var articleNumber = feedIndex * articlesPerFeed + articleIndex;
        feed.Articles.Add(new Article
        {
            Id = $"scale-{articleNumber:000000}",
            Link = $"https://scale.test/article/{articleNumber:000000}",
            Title = $"Android articol {articleNumber:000000}",
            Content = "Conținut sintetic pentru verificarea colecțiilor mari.",
            Published = now.AddDays(-(articleIndex % 90)),
            IsFavorite = articleIndex % 1000 == 0,
            ReadLater = articleIndex % 997 == 0
        });
    }
}

Check(feeds.Count == feedCount, "numărul de feeduri sintetice");
Check(feeds.Sum(feed => feed.Articles.Count) == expectedArticles, "numărul de articole sintetice");

var orderedFeeds = FeedListNavigation.Order(feeds);
Check(orderedFeeds[0].Name == "Feed 000" && orderedFeeds[^1].Name == "Feed 099", "ordonare stabilă a feedurilor");
Check(FeedListNavigation.FindNextByInitial(orderedFeeds, 'F', 0) == 1, "navigare cu inițială în lista mare");
var flatArticles = orderedFeeds.SelectMany(feed => feed.Articles).ToList();
Check(FeedListNavigation.FindNextArticleByInitial(flatArticles, 'A', 0) == 1, "navigare cu inițială în articole multe");

var cutoff = now.AddDays(-30);
var expectedExpired = feeds.Sum(feed => feed.Articles.Count(article => !ArticleRetention.ShouldKeep(article, cutoff)));
Check(ArticleRetention.CountExpired(feeds, cutoff) == expectedExpired, "numărare retenție pe colecție mare");
var removed = ArticleRetention.RemoveExpired(feeds, cutoff);
Check(removed == expectedExpired, "ștergere retenție pe colecție mare");
Check(feeds.Sum(feed => feed.Articles.Count) == expectedArticles - removed, "articole protejate și recente păstrate");
Check(feeds.SelectMany(feed => feed.Articles).All(article => ArticleRetention.ShouldKeep(article, cutoff)), "nicio expirare rămasă");

var duplicateInput = Enumerable.Range(0, expectedArticles)
    .Select(index => new Article
    {
        Id = $"duplicate-{index:000000}",
        Link = index < 1000
            ? $"https://scale.test/duplicate/{index % 500:000}"
            : $"https://scale.test/unique/{index:000000}",
        Title = $"Duplicat de test {index:000000}",
        Published = now.AddMinutes(-index)
    })
    .ToList();
var distinct = DuplicateCleaner.DeduplicateArticles(duplicateInput, out var duplicateCount);
Check(duplicateCount == 500 && distinct.Count == expectedArticles - 500, "deduplicare pe colecție mare");

var json = JsonSerializer.Serialize(feeds, new JsonSerializerOptions { WriteIndented = false });
var roundTrip = JsonSerializer.Deserialize<List<Feed>>(json) ?? [];
Check(json.Length > 1_000_000, "serializare semnificativă a colecției");
Check(roundTrip.Count == feedCount && roundTrip.Sum(feed => feed.Articles.Count) == expectedArticles - removed, "round-trip JSON pentru colecție mare");

// Baseline A11: full-length article text, local search, JSON snapshot disk save,
// and TXT/RTF export. All files are synthetic and isolated under a unique temp directory.
const int fullArticleCount = 24;
const int fullArticleChars = 262_144;
const string paragraph = "Știre sintetică pentru test de performanță: tehnologie Android, accesibilitate și citire prin tastatură. ";
var repeatedParagraphs = string.Concat(Enumerable.Repeat(paragraph, fullArticleChars / paragraph.Length + 1));
var fullText = repeatedParagraphs[..fullArticleChars];
var fullTextArticles = Enumerable.Range(0, fullArticleCount)
    .Select(index => new Article
    {
        Id = $"a11-{index:000}",
        Title = $"Articol sintetic complet {index:000}",
        Content = fullText,
        FullContent = fullText,
        Link = $"https://scale.test/a11/{index:000}",
        Published = now,
        SourceName = "Feed de test A11"
    })
    .ToList();
var searchTimes = new List<double>();
var matchedArticles = 0;
for (var sample = 0; sample < 3; sample++)
{
    var searchTimer = Stopwatch.StartNew();
    matchedArticles += fullTextArticles.Count(article => ArticleSearch.Matches(article, "android accesibilitate"));
    searchTimer.Stop();
    searchTimes.Add(searchTimer.Elapsed.TotalMilliseconds);
}
Check(matchedArticles == fullArticleCount * 3, "căutarea pe articole cu text integral mare găsește toate potrivirile");

var tempRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
var benchmarkDirectory = Path.Combine(tempRoot, $"OrizontRSS-ScaleSmoke-{Guid.NewGuid():N}");
Directory.CreateDirectory(benchmarkDirectory);
try
{
    var benchmarkFeed = new Feed
    {
        Name = "Feed sintetic A11",
        Url = "https://scale.test/a11/feed",
        Articles = fullTextArticles
    };
    var saveOptions = new JsonSerializerOptions { WriteIndented = true };
    var serializeTimes = new List<double>();
    var diskSaveTimes = new List<double>();
    var snapshotBytes = 0;
    var snapshotPath = Path.Combine(benchmarkDirectory, "feeds.json");
    for (var sample = 0; sample < 3; sample++)
    {
        var serializeTimer = Stopwatch.StartNew();
        var snapshot = JsonSerializer.SerializeToUtf8Bytes(new[] { benchmarkFeed }, saveOptions);
        serializeTimer.Stop();
        serializeTimes.Add(serializeTimer.Elapsed.TotalMilliseconds);
        snapshotBytes = snapshot.Length;

        var temporaryPath = Path.Combine(benchmarkDirectory, "feeds.json.tmp");
        var diskTimer = Stopwatch.StartNew();
        File.WriteAllBytes(temporaryPath, snapshot);
        File.Move(temporaryPath, snapshotPath, overwrite: true);
        diskTimer.Stop();
        diskSaveTimes.Add(diskTimer.Elapsed.TotalMilliseconds);
    }
    var savedFeeds = JsonSerializer.Deserialize<List<Feed>>(File.ReadAllBytes(snapshotPath)) ?? [];
    Check(snapshotBytes > 1_000_000 && savedFeeds.Count == 1 && savedFeeds[0].Articles.Count == fullArticleCount,
        "salvarea locală izolată păstrează snapshotul cu articole integrale mari");
    Check(savedFeeds[0].Articles.All(article => article.Content.Length == fullArticleChars && article.FullContent?.Length == fullArticleChars),
        "round-trip-ul snapshotului păstrează conținutul complet al articolelor");

    var exportArticle = fullTextArticles[0];
    var txtTimes = new List<double>();
    var rtfTimes = new List<double>();
    var txtPath = string.Empty;
    var rtfPath = string.Empty;
    for (var sample = 0; sample < 3; sample++)
    {
        var txtTimer = Stopwatch.StartNew();
        txtPath = ArticleExportService.Save(exportArticle, fullText, benchmarkDirectory, "Feed de test A11", "txt");
        txtTimer.Stop();
        txtTimes.Add(txtTimer.Elapsed.TotalMilliseconds);

        var rtfTimer = Stopwatch.StartNew();
        rtfPath = ArticleExportService.Save(exportArticle, fullText, benchmarkDirectory, "Feed de test A11", "rtf");
        rtfTimer.Stop();
        rtfTimes.Add(rtfTimer.Elapsed.TotalMilliseconds);
    }
    Check(File.ReadAllText(txtPath).Contains(exportArticle.Link, StringComparison.Ordinal), "exportul TXT păstrează sursa după salvarea textului mare");
    Check(File.ReadAllText(rtfPath).Contains(exportArticle.Link, StringComparison.Ordinal), "exportul RTF păstrează sursa după salvarea textului mare");

    var searchMedian = searchTimes.Order().ElementAt(searchTimes.Count / 2);
    var serializeMedian = serializeTimes.Order().ElementAt(serializeTimes.Count / 2);
    var diskMedian = diskSaveTimes.Order().ElementAt(diskSaveTimes.Count / 2);
    var txtMedian = txtTimes.Order().ElementAt(txtTimes.Count / 2);
    var rtfMedian = rtfTimes.Order().ElementAt(rtfTimes.Count / 2);
    Console.WriteLine($"A11 synthetic baseline: {fullArticleCount} articles x {fullArticleChars:N0} chars per Content and FullContent; search median {searchMedian:F1} ms; JSON serialize median {serializeMedian:F1} ms; temp snapshot replace median {diskMedian:F1} ms for {snapshotBytes:N0} bytes; TXT export median {txtMedian:F1} ms; RTF export median {rtfMedian:F1} ms. Three samples each; no user profile or network.");
}
finally
{
    var resolvedBenchmarkDirectory = Path.GetFullPath(benchmarkDirectory);
    if (Directory.Exists(resolvedBenchmarkDirectory) &&
        string.Equals(Path.GetDirectoryName(resolvedBenchmarkDirectory), tempRoot, StringComparison.OrdinalIgnoreCase) &&
        Path.GetFileName(resolvedBenchmarkDirectory).StartsWith("OrizontRSS-ScaleSmoke-", StringComparison.Ordinal))
        Directory.Delete(resolvedBenchmarkDirectory, recursive: true);
}

// Exercise the actual article ListBox and its XAML template without showing the
// application window or raising Window_Loaded (which would load the user profile).
const int wpfListArticleCount = 1_000;
var wpfListArticles = Enumerable.Range(0, wpfListArticleCount)
    .Select(index => new Article
    {
        Id = $"wpf-{index:0000}",
        Title = $"Articol sintetic pentru lista {index:0000}",
        Content = index % 40 == 0 ? fullText : "Conținut sintetic scurt.",
        FullContent = index % 40 == 0 ? fullText : null,
        Link = $"https://scale.test/wpf/{index:0000}",
        Published = now.AddMinutes(-index),
        SourceName = "Feed WPF sintetic",
        IncludeSourceInDisplay = true,
        DisplayIndex = index + 1,
        DisplayTotal = wpfListArticleCount
    })
    .ToList();
var wpfItemCount = 0;
var wpfRealizedContainerCount = 0;
var wpfHasVirtualizingPanel = false;
var wpfLayoutMilliseconds = 0d;
Exception? wpfFailure = null;
var wpfThread = new Thread(() =>
{
    try
    {
        var app = new App();
        app.InitializeComponent();
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var window = new MainWindow();
        var articlesField = typeof(MainWindow).GetField("Articles", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Nu s-a găsit lista Articole din fereastra principală.");
        var articleList = articlesField.GetValue(window) as ListBox
            ?? throw new InvalidOperationException("Controlul Articole nu este un ListBox.");
        var root = window.Content as UIElement
            ?? throw new InvalidOperationException("Fereastra nu are conținut WPF măsurabil.");

        var layoutTimer = Stopwatch.StartNew();
        articleList.ItemsSource = wpfListArticles;
        window.ApplyTemplate();
        root.Measure(new Size(1200, 720));
        root.Arrange(new Rect(0, 0, 1200, 720));
        root.UpdateLayout();
        articleList.UpdateLayout();
        layoutTimer.Stop();

        wpfLayoutMilliseconds = layoutTimer.Elapsed.TotalMilliseconds;
        wpfItemCount = articleList.Items.Count;
        wpfRealizedContainerCount = Enumerable.Range(0, wpfItemCount)
            .Count(index => articleList.ItemContainerGenerator.ContainerFromIndex(index) is ListBoxItem);
        wpfHasVirtualizingPanel = FindVirtualizingPanel(articleList);
        app.Shutdown();
    }
    catch (Exception exception)
    {
        wpfFailure = exception;
    }
});
wpfThread.SetApartmentState(ApartmentState.STA);
wpfThread.Start();
wpfThread.Join();
if (wpfFailure is not null)
    throw new InvalidOperationException("Măsurarea WPF izolată a eșuat.", wpfFailure);
Check(wpfItemCount == wpfListArticleCount, "lista WPF reală primește articolele sintetice fără profil");
Check(wpfRealizedContainerCount > 0 && wpfRealizedContainerCount < wpfItemCount, "virtualizarea limitează rândurile WPF materializate la cele vizibile");
Check(wpfHasVirtualizingPanel, "lista Articole folosește panoul WPF de virtualizare");
Console.WriteLine($"A11 WPF off-screen layout: {wpfItemCount:N0} synthetic articles in {wpfLayoutMilliseconds:F1} ms; {wpfRealizedContainerCount:N0} realized row containers; VirtualizingStackPanel={wpfHasVirtualizingPanel}; window not shown, user profile not loaded.");

timer.Stop();
Console.WriteLine($"Scale smoke passed: {checks} checks, {expectedArticles:N0} synthetic articles, {json.Length:N0} JSON chars, {timer.Elapsed.TotalSeconds:F2}s. No network or user data accessed.");

static bool FindVirtualizingPanel(DependencyObject root)
{
    if (root is VirtualizingStackPanel) return true;
    var childCount = VisualTreeHelper.GetChildrenCount(root);
    for (var index = 0; index < childCount; index++)
        if (FindVirtualizingPanel(VisualTreeHelper.GetChild(root, index))) return true;
    return false;
}
