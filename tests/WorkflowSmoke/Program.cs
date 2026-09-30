using System.Reflection;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using CititorRSS.Jaws;
using CititorRSS.Jaws.Localization;
using CititorRSS.Jaws.Services;
using CititorRSS.Jaws.Services.Content;

internal static class Program
{
    private static int checks;
    private static readonly List<string> failures = [];
    [STAThread]
    private static int Main()
    {
        try
        {
            // No App.Run, Window.Show or Loaded: never read the user's profile.
            var app = new App(); app.InitializeComponent(); UiCulture.Apply("ro-RO");
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
            var task = RunAsync();
            if (!task.IsCompleted)
            {
                var frame = new DispatcherFrame();
                task.GetAwaiter().OnCompleted(() => frame.Continue = false);
                Dispatcher.PushFrame(frame);
            }
            task.GetAwaiter().GetResult();
            foreach (var failure in failures) Console.Error.WriteLine("FAIL: " + failure);
            Console.WriteLine($"WorkflowSmoke: {checks - failures.Count}/{checks} passed. Synthetic articles and callbacks only; no visible windows, profile access or network.");
            return failures.Count == 0 ? 0 : 1;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }

    private static void Check(bool ok, string name) { checks++; if (!ok) failures.Add(name); }
    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(target, value);
    private static void Select(MainWindow window, Article article)
    {
        ((ListBox)window.FindName("Articles")).SelectedItem = article;
    }

    private sealed class Fixture
    {
        public MainWindow Window { get; } = new();
        public Article A { get; } = new() { Id = "a", Title = "Article A", Link = "https://example.test/a", Content = "Summary A" };
        public Article B { get; } = new() { Id = "b", Title = "Article B", Link = "https://example.test/b", Content = "Summary B" };
        public AppSettings Settings { get; } = new();
        public Func<string, Task<string>> Load { get; set; } = _ => Task.FromResult(new string('a', 200));
        public Func<Task<string>> Translate { get; set; } = () => Task.FromResult("Translated A");
        public Func<CancellationToken, Task<string>> GoogleTranslator { get; set; } = _ => Task.FromResult("Translated A");
        public Func<Task> Save { get; set; } = () => Task.CompletedTask;
        public int Saves, Errors, GoogleCalls;
        public bool ConfirmGoogleResult = true;
        public Article? Opened;
        public string? TranslationTitle, TranslationLink;
        public string? DeepLSource;
        public ArticleTranslationResult? TranslationResult;
        public Func<Task<string>>? Refresh;
        public TextBox Reader => (TextBox)Window.FindName("Reader");
        public Fixture()
        {
            Set(Window, "_feeds", new List<Feed> { new() { Name = "Synthetic", Articles = [A, B] } });
            Set(Window, "_settings", Settings);
            ((ListBox)Window.FindName("Articles")).ItemsSource = new[] { A, B };
            Select(Window, A);
            Window.ArticleCommands = new ArticleCommandPorts
            {
                LoadReadableAsync = link => Load(link),
                LoadNewsBlurTextAsync = async (_, _) => await Load(A.Link),
                Unprotect = _ => "synthetic-key-not-used-online",
                TranslateDeepLAsync = (_, text, _) => { DeepLSource = text; return Translate(); },
                TranslateGoogleAsync = (_, _, _, cancellationToken) => { GoogleCalls++; return GoogleTranslator(cancellationToken); },
                SaveAsync = async () => { Saves++; await Save(); },
                ShowReader = (article, _, refresh) => { Opened = article; Refresh = refresh; },
                ShowTranslation = result => { TranslationResult = result; TranslationTitle = result.Title; TranslationLink = result.Link; },
                ConfirmGoogle = () => ConfirmGoogleResult,
                ShowError = (_, _, _) => Errors++
            };
        }
    }

    private static async Task RunAsync()
    {
        var mutexName = $@"Local\OrizontRSS.WorkflowSmoke.{Guid.NewGuid():N}";
        var primaryInstance = SingleInstanceGuard.TryAcquire(mutexName);
        Check(primaryInstance is not null, "first process acquires the single-instance guard");
        var secondInstanceAcquired = true;
        var contender = new Thread(() =>
        {
            using var secondInstance = SingleInstanceGuard.TryAcquire(mutexName);
            secondInstanceAcquired = secondInstance is not null;
        });
        contender.Start(); contender.Join();
        Check(!secondInstanceAcquired, "a second process cannot acquire the profile guard");
        primaryInstance!.Dispose();
        using var reopenedInstance = SingleInstanceGuard.TryAcquire(mutexName);
        Check(reopenedInstance is not null, "the guard is released when the primary process exits");

        var firstGoogleLease = GoogleTranslateRequestGate.TryAcquire();
        var duplicateGoogleLease = GoogleTranslateRequestGate.TryAcquire();
        Check(firstGoogleLease is not null && duplicateGoogleLease is null, "Google Translate gate blocks overlapping requests");
        firstGoogleLease?.Dispose();
        var reopenedGoogleLease = GoogleTranslateRequestGate.TryAcquire();
        Check(reopenedGoogleLease is not null, "Google Translate gate becomes available after release");
        reopenedGoogleLease?.Dispose();

        using (var sharedSpeech = new SpeechService())
        {
            var voiceArticle = new Article { Id = "voice-route", Title = "Synthetic voice route", Link = "https://example.test/voice" };
            var articleReader = new ArticleReaderWindow(
                voiceArticle,
                "Synthetic article body",
                new AppSettings(),
                () => Task.FromResult("Synthetic article body"),
                () => Task.CompletedTask,
                sharedSpeech);
            var createResponse = typeof(ArticleReaderWindow).GetMethod("CreateAiResponseWindow", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var responseWindow = (AiResponseWindow)createResponse.Invoke(articleReader, new object?[]
            {
                "Rezumat",
                "Synthetic AI response",
                "Gemini",
                false,
                new Func<string, Task<string>>(_ => Task.FromResult("Synthetic follow-up")),
                new Func<string, Task>(_ => Task.CompletedTask),
                "Synthetic article body",
                "English"
            })!;
            var responseSpeech = typeof(AiResponseWindow).GetField("_speech", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(responseWindow);
            Check(ReferenceEquals(sharedSpeech, responseSpeech), "Cititor Orizont passes its shared voice service into the AI response window");

            var missingSpeechRejected = false;
            try
            {
                _ = new AiResponseWindow("Rezumat", "Răspuns", "Articol", "https://example.test/a", _ => Task.FromResult(""), _ => Task.CompletedTask, null!);
            }
            catch (ArgumentNullException) { missingSpeechRejected = true; }
            Check(missingSpeechRejected, "AI response window rejects a missing voice service");
        }

        var f = new Fixture();
        var delayed = new TaskCompletionSource<string>();
        f.Load = _ => delayed.Task;
        var opening = f.Window.OpenArticleInOrizontAsync();
        Select(f.Window, f.B);
        delayed.SetResult(new string('a', 200)); await opening;
        Check(f.B.FullContent is null && !f.B.IsRead, "opening A cannot write text/read state to B after selection changes");
        Check(f.Opened is null || ReferenceEquals(f.Opened, f.A), "reader never opens B with A's text");

        f = new Fixture();
        await f.Window.OpenArticleInOrizontAsync();
        Check(f.Opened == f.A && f.A.IsRead && f.Saves > 0, "ordinary reader opens A and preserves mark-read/save");
        Select(f.Window, f.B);
        string? requested = null;
        f.Load = link => { requested = link; return Task.FromResult("Refreshed A"); };
        await f.Refresh!();
        Check(requested == f.A.Link && f.A.FullContent == "Refreshed A" && f.B.FullContent is null, "existing reader refresh remains bound to A");

        f = new Fixture(); delayed = new(); f.Load = _ => delayed.Task;
        var full = f.Window.LoadFullArticleAsync(); Select(f.Window, f.B);
        delayed.SetResult(new string('a', 200)); await full;
        Check(f.Reader.Text == f.B.Content && f.B.FullContent is null && f.Saves == 0, "superseded full-text completion neither overwrites B nor starts a save");

        f = new Fixture(); delayed = new(); f.Translate = () => delayed.Task;
        f.Settings.DeepLEnabled = true;
        var deepL = f.Window.TranslateDeepLAsync(); Select(f.Window, f.B);
        delayed.SetResult("Translated A"); await deepL;
        Check(f.Reader.Text == f.B.Content, "late DeepL translation cannot replace B's panel");

        f = new Fixture(); delayed = new(); f.GoogleTranslator = _ => delayed.Task;
        var google = f.Window.TranslateGoogleAsync(); Select(f.Window, f.B);
        delayed.SetResult("Translated A"); await google;
        Check(f.TranslationLink is null || f.TranslationLink == f.A.Link, "Google result never uses B's source for A's text");

        f = new Fixture(); f.A.FullContent = "Original full article"; delayed = new(); f.GoogleTranslator = _ => delayed.Task;
        var firstGoogle = f.Window.TranslateGoogleAsync();
        var duplicateGoogle = f.Window.TranslateGoogleAsync();
        Check(f.GoogleCalls == 1, "rapid Google Translate activations start one request only");
        Check(((TextBlock)f.Window.FindName("Status")).Text.Contains("Google Translate"), "duplicate activation announces the in-progress Google translation");
        delayed.SetResult("Translated once");
        await Task.WhenAll(firstGoogle, duplicateGoogle);
        await f.Window.TranslateGoogleAsync();
        Check(f.GoogleCalls == 2 && f.TranslationResult?.TranslatedText == "Translated once", "Google Translate can be started again after a successful request");

        f = new Fixture { ConfirmGoogleResult = false };
        f.A.FullContent = "Original full article";
        await f.Window.TranslateGoogleAsync();
        Check(f.GoogleCalls == 0 && ((TextBlock)f.Window.FindName("Status")).Text.Contains("anulată"), "declining Google confirmation announces cancellation without sending a request");
        f.ConfirmGoogleResult = true;
        await f.Window.TranslateGoogleAsync();
        Check(f.GoogleCalls == 1 && f.TranslationResult is not null, "Google Translate can be started again after consent is declined");

        f = new Fixture(); f.A.FullContent = "Original full article";
        f.GoogleTranslator = _ => Task.FromException<string>(new InvalidOperationException("synthetic Google failure"));
        await f.Window.TranslateGoogleAsync();
        Check(f.Errors == 1 && f.TranslationResult is null, "Google translation failure is reported without showing a result");
        f.GoogleTranslator = _ => Task.FromResult("Recovered translation");
        await f.Window.TranslateGoogleAsync();
        Check(f.GoogleCalls == 2 && f.TranslationResult?.TranslatedText == "Recovered translation", "Google Translate can be started again after an error");

        f = new Fixture(); f.A.FullContent = "Original full article";
        CancellationToken observedGoogleToken = default;
        f.GoogleTranslator = async token =>
        {
            observedGoogleToken = token;
            await Task.Delay(Timeout.Infinite, token);
            return "Must not be displayed";
        };
        var closingGoogle = f.Window.TranslateGoogleAsync();
        Check(observedGoogleToken.CanBeCanceled, "main-window Google translation passes a cancellation token to the service");
        Set(f.Window, "_closeInProgress", true);
        typeof(MainWindow).GetMethod("CancelGoogleTranslationForShutdown", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(f.Window, null);
        await closingGoogle;
        Check(f.TranslationResult is null && f.Errors == 0, "closing the main window cancels Google translation without displaying a result or error");
        var releasedGoogleLease = GoogleTranslateRequestGate.TryAcquire();
        Check(releasedGoogleLease is not null, "Google Translate request gate is released after cancellation");
        releasedGoogleLease?.Dispose();

        f = new Fixture(); f.A.FullContent = "Original full article"; f.Reader.Text = "Translated display panel";
        await f.Window.TranslateGoogleAsync();
        Check(f.TranslationTitle == f.A.Title && f.TranslationLink == f.A.Link, "ordinary Google translation retains article title and source");
        Check(f.TranslationResult?.OriginalText == f.A.FullContent && f.TranslationResult?.Provider == "Google Translate" && !string.IsNullOrWhiteSpace(f.TranslationResult.Language),
            "Google translation window receives original text, provider, source and target-language provenance");

        f = new Fixture(); f.Reader.Text = "Previously translated panel"; f.Load = _ => Task.FromException<string>(new InvalidOperationException("synthetic refresh failure"));
        await f.Window.TranslateGoogleAsync();
        Check(f.TranslationResult?.OriginalText == f.A.Content, "Google translation falls back to the original article rather than a translated panel");

        f = new Fixture(); f.A.FullContent = "Original full article"; f.Reader.Text = "Translated display panel"; f.Settings.DeepLEnabled = true;
        await f.Window.TranslateDeepLAsync();
        Check(f.Reader.Text == "Translated A", "ordinary DeepL result is still displayed");
        Check((string?)typeof(MainWindow).GetField("_translationProvider", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(f.Window) == "DeepL", "DeepL attribution survives the identity fix");
        Check(f.DeepLSource == f.A.FullContent, "main-window DeepL uses the original article text rather than a translated display panel");
        Check((string?)typeof(MainWindow).GetField("_translationLanguage", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(f.Window) is not null, "DeepL translation context records the target language");

        f = new Fixture(); f.A.FullContent = "Original full article";
        Set(f.Window, "_translationProvider", "DeepL"); Set(f.Window, "_translationLanguage", "English");
        await f.Window.LoadFullArticleAsync();
        Check(f.Reader.Text == f.A.FullContent && (string?)typeof(MainWindow).GetField("_translationProvider", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(f.Window) is null,
            "restoring original full text clears translation provenance");
        Check((string?)typeof(MainWindow).GetField("_translationLanguage", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(f.Window) is null,
            "restoring original full text clears the translated target language");

        f = new Fixture();
        f.A.FullContent = "Original full article";
        f.Reader.Text = "Traducere DeepL";
        Set(f.Window, "_translationProvider", "DeepL");
        Set(f.Window, "_translationLanguage", "English");
        var mainDistributionContext = (ArticleDistributionContext)typeof(MainWindow)
            .GetMethod("CreateArticleDistributionContext", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(f.Window, new object?[] { f.A, f.Reader.Text })!;
        var mainDistributionNote = mainDistributionContext.BuildTranslationNote("Traducere automată realizată prin {0} în limba {1}.");
        var mainDistributionDocument = ArticleSharing.BuildShareText(mainDistributionContext, mainDistributionNote,
            "Conținut preluat prin Orizont RSS:", "Sursa articolului:");
        Check(mainDistributionContext.OriginalText == "Original full article" && mainDistributionContext.DisplayText == "Traducere DeepL",
            "main-window distribution keeps original and displayed translation distinct");
        Check(mainDistributionDocument.Contains("DeepL") && mainDistributionDocument.Contains("English") &&
              mainDistributionDocument.Contains(ArticleSharing.PresentationUrl) && mainDistributionDocument.Contains(f.A.Link),
            "main-window translated distribution contains provider, language, app link and source");

        using (var distributionSpeech = new SpeechService())
        {
            var readerArticle = new Article
            {
                Id = "reader-distribution", Title = "Reader article", Link = "https://example.test/reader-distribution",
                Content = "Short summary", FullContent = "Original reader article"
            };
            var articleReader = new ArticleReaderWindow(readerArticle, readerArticle.FullContent!, new AppSettings(),
                () => Task.FromResult(readerArticle.FullContent!), () => Task.CompletedTask, distributionSpeech);
            ((TextBox)articleReader.FindName("ArticleText")).Text = "Traducere Reader";
            Set(articleReader, "_translationProvider", "DeepL");
            Set(articleReader, "_translationLanguage", "română");
            var readerContext = (ArticleDistributionContext)typeof(ArticleReaderWindow)
                .GetMethod("CreateArticleDistributionContext", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(articleReader, new object?[] { "Traducere Reader" })!;
            Check(readerContext.OriginalText == "Original reader article" && readerContext.DisplayText == "Traducere Reader" &&
                  readerContext.SourceUrl == readerArticle.Link && readerContext.TranslationProvider == "DeepL",
                "Cititor Orizont distribution context preserves original, translated display and source");

            var readerContextMenu = (ContextMenu)articleReader.Resources["ReaderContextMenu"];
            var contextSaveMenu = readerContextMenu.Items.OfType<MenuItem>()
                .FirstOrDefault(item => string.Equals(item.Header?.ToString(), "Salvează articolul", StringComparison.Ordinal));
            var readerMenu = (Menu)articleReader.FindName("ReaderMenu");
            var topLevelReaderMenu = readerMenu.Items.OfType<MenuItem>()
                .FirstOrDefault(item => string.Equals(item.Header?.ToString(), "Cititor", StringComparison.Ordinal));
            Check(contextSaveMenu?.Items.OfType<MenuItem>().Select(item => item.Header?.ToString()).SequenceEqual(["Salvează ca TXT", "Salvează ca RTF"]) == true,
                "Cititor Orizont context menu exposes TXT and RTF save commands");
            Check(topLevelReaderMenu?.Items.OfType<MenuItem>().Any(item => string.Equals(item.Header?.ToString(), "Salvează articolul", StringComparison.Ordinal)) == true,
                "Cititor Orizont top menu exposes article saving as an accessible alternative");

            var previousCulture = CultureInfo.CurrentUICulture.Name;
            try
            {
                UiCulture.Apply("en-US");
                var englishReader = new ArticleReaderWindow(readerArticle, "Original reader article", new AppSettings(),
                    () => Task.FromResult("Original reader article"), () => Task.CompletedTask, distributionSpeech);
                var englishContext = (ContextMenu)englishReader.Resources["ReaderContextMenu"];
                var englishSaveMenu = englishContext.Items.OfType<MenuItem>()
                    .FirstOrDefault(item => string.Equals(item.Header?.ToString(), "Save article", StringComparison.Ordinal));
                var englishMainMenu = (Menu)englishReader.FindName("ReaderMenu");
                Check(englishSaveMenu?.Items.OfType<MenuItem>().Select(item => item.Header?.ToString()).SequenceEqual(["Save as TXT", "Save as RTF"]) == true &&
                      englishMainMenu.Items.OfType<MenuItem>().SelectMany(item => item.Items.OfType<MenuItem>())
                          .Any(item => string.Equals(item.Header?.ToString(), "Save article", StringComparison.Ordinal)),
                    "Cititor Orizont save menu labels follow the selected English interface language");
            }
            finally { UiCulture.Apply(previousCulture); }

            var exportFolder = Path.Combine(Path.GetTempPath(), "OrizontRSS-WorkflowSmoke-" + Guid.NewGuid().ToString("N"));
            var readerSettings = new AppSettings { ArticleExportFolder = exportFolder };
            var exportReader = new ArticleReaderWindow(readerArticle, "Original reader article", readerSettings,
                () => Task.FromResult("Original reader article"), () => Task.CompletedTask, distributionSpeech);
            ((TextBox)exportReader.FindName("ArticleText")).Text = "Traducere Reader";
            Set(exportReader, "_translationProvider", "DeepL");
            Set(exportReader, "_translationLanguage", "română");
            try
            {
                typeof(ArticleReaderWindow).GetMethod("SaveArticleAsTxt_Click", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .Invoke(exportReader, new object?[] { exportReader, new RoutedEventArgs() });
                typeof(ArticleReaderWindow).GetMethod("SaveArticleAsRtf_Click", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .Invoke(exportReader, new object?[] { exportReader, new RoutedEventArgs() });
                var txtPath = Directory.GetFiles(exportFolder, "*.txt").Single();
                var rtfPath = Directory.GetFiles(exportFolder, "*.rtf").Single();
                var txtDocument = File.ReadAllText(txtPath);
                var rtfDocument = File.ReadAllText(rtfPath);
                Check(txtDocument.Contains("Traducere Reader") && txtDocument.Contains("DeepL") && txtDocument.Contains("română") &&
                      txtDocument.Contains(ArticleSharing.PresentationUrl) && txtDocument.Contains(readerArticle.Link),
                    "Cititor Orizont TXT export preserves displayed translation, provider, language and provenance");
                Check(rtfDocument.Contains("DeepL") && rtfDocument.Contains(ArticleSharing.PresentationUrl) && rtfDocument.Contains(readerArticle.Link),
                    "Cititor Orizont RTF export preserves provider and provenance");
            }
            finally
            {
                if (Directory.Exists(exportFolder)) Directory.Delete(exportFolder, recursive: true);
            }

            var translationWindow = new ArticleTranslationWindow("Traducere Google", "Google article",
                "https://example.test/google-article", "Google Translate", "Original Google article", "română");
            var translationShare = (string)typeof(ArticleTranslationWindow)
                .GetMethod("ShareText", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(translationWindow, null)!;
            Check(translationShare.Contains("Traducere Google") && translationShare.Contains("Google Translate") &&
                  translationShare.Contains("română") && translationShare.Contains(ArticleSharing.PresentationUrl) &&
                  translationShare.Contains("https://example.test/google-article") && !translationShare.Contains("Original Google article"),
                "Google translation window shares the displayed translation with provider, language and source");

            var translatedAiWindow = new AiResponseWindow("Traducere", "Răspuns tradus", "AI article",
                "https://example.test/ai-article", _ => Task.FromResult("Follow-up"), _ => Task.CompletedTask,
                distributionSpeech, "DeepSeek", true, "Original AI article", "română");
            var translatedAiDocument = (string)typeof(AiResponseWindow)
                .GetMethod("ResponseDocument", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(translatedAiWindow, null)!;
            Check(translatedAiDocument.Contains("Răspuns tradus") && translatedAiDocument.Contains("DeepSeek") &&
                  translatedAiDocument.Contains("română") && translatedAiDocument.Contains(ArticleSharing.PresentationUrl) &&
                  translatedAiDocument.Contains("https://example.test/ai-article"),
                "AI translation document keeps provider, target language, app link and source");

            var ordinaryAiWindow = new AiResponseWindow("Rezumat", "Răspuns obișnuit", "AI article",
                "https://example.test/ai-article", _ => Task.FromResult("Follow-up"), _ => Task.CompletedTask,
                distributionSpeech, "DeepSeek", false, "Original AI article");
            var ordinaryAiDocument = (string)typeof(AiResponseWindow)
                .GetMethod("ResponseDocument", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(ordinaryAiWindow, null)!;
            Check(ordinaryAiDocument.Contains(ArticleSharing.PresentationUrl) && ordinaryAiDocument.Contains("https://example.test/ai-article") &&
                  !ordinaryAiDocument.Contains("Traducere automată realizată prin DeepSeek"),
                "ordinary AI response keeps app/source attribution without a false translation label");
        }

        f = new Fixture(); delayed = new(); f.Load = _ => delayed.Task;
        full = f.Window.LoadFullArticleAsync(); Select(f.Window, f.B); Select(f.Window, f.A);
        delayed.SetResult(new string('a', 200)); await full;
        Check(f.Reader.Text == f.A.Content && f.A.FullContent is null, "A-B-A selection invalidates the old operation too");

        f = new Fixture(); delayed = new(); f.Load = _ => delayed.Task;
        full = f.Window.LoadFullArticleAsync();
        f.Load = _ => Task.FromResult(new string('n', 200));
        await f.Window.LoadFullArticleAsync();
        delayed.SetResult(new string('o', 200)); await full;
        Check(f.Reader.Text == new string('n', 200) && f.A.FullContent == f.Reader.Text, "an older full-text response cannot overwrite a newer response for A");

        f = new Fixture(); var saveDelay = new TaskCompletionSource(); f.Save = () => saveDelay.Task;
        full = f.Window.LoadFullArticleAsync(); Select(f.Window, f.B);
        saveDelay.SetResult(); await full;
        Check(f.A.FullContent?.Length == 200 && f.B.FullContent is null && f.Reader.Text == f.B.Content, "selection change during persistence cannot overwrite B's panel");

        f = new Fixture(); delayed = new(); f.Load = _ => delayed.Task;
        opening = f.Window.OpenArticleInOrizontAsync();
        ((ListBox)f.Window.FindName("Articles")).SelectedIndex = -1;
        delayed.SetResult(new string('a', 200)); await opening;
        Check(f.Opened is null && f.Errors == 0 && !f.A.IsRead, "cleared selection does not throw or open a stale reader");

        f = new Fixture(); delayed = new(); f.Load = _ => delayed.Task;
        opening = f.Window.OpenArticleInOrizontAsync(); Select(f.Window, f.B);
        delayed.SetException(new InvalidOperationException("simulated failure")); await opening;
        Check(f.Errors == 0 && f.Reader.Text == f.B.Content, "late failure cannot steal focus with an unrelated error dialog");

        f = new Fixture(); f.Load = _ => Task.FromException<string>(new InvalidOperationException("simulated failure"));
        await f.Window.OpenArticleInOrizontAsync();
        Check(f.Errors == 1 && f.Opened is null, "current-article failure still reports an error");

        f = new Fixture(); f.A.FullContent = "Previously cached A";
        f.Load = _ => Task.FromException<string>(new InvalidOperationException("offline"));
        await f.Window.OpenArticleInOrizontAsync();
        Check(f.Opened == f.A && f.A.FullContent == "Previously cached A" && f.Errors == 0, "reader keeps the cached-article fallback");

        f = new Fixture(); var reads = 0; f.A.FullContent = "Cached A";
        f.Load = _ => { reads++; throw new Exception("Should not read"); };
        await f.Window.LoadFullArticleAsync();
        Check(reads == 0 && f.Reader.Text == "Cached A", "cached full text does not require a new download");

        f = new Fixture(); delayed = new(); f.Load = _ => delayed.Task;
        opening = f.Window.OpenArticleInOrizontAsync(); Set(f.Window, "_closeInProgress", true);
        delayed.SetResult(new string('a', 200)); await opening;
        Check(f.Opened is null && f.Saves == 0, "closing suppresses late article presentation and saves");

        foreach (var connected in new[] { false, true })
        foreach (var session in new[] { false, true })
        foreach (var feeds in new[] { false, true })
        foreach (var articles in new[] { false, true })
        foreach (var rss in new[] { false, true })
        foreach (var periodic in new[] { false, true })
        {
            var window = new MainWindow();
            Set(window, "_settings", new AppSettings
            {
                NewsBlurConnected = connected, EncryptedNewsBlurSession = session ? "fake-session-not-decrypted" : null,
                NewsBlurSyncFeedsAndFoldersAtStartup = feeds, NewsBlurSyncArticlesAndStatesAtStartup = articles,
                UpdateAtStartup = rss, NewsBlurAutoSyncEnabled = periodic
            });
            var calls = new List<string>();
            await window.RunStartupUpdatesAsync(() => { calls.Add("feeds"); return Task.CompletedTask; },
                newsBlur => { calls.Add(newsBlur ? "newsblur-articles" : "rss"); return Task.CompletedTask; });
            var expected = new List<string>();
            if (connected && session && feeds) expected.Add("feeds");
            if (connected && session && articles) expected.Add("newsblur-articles");
            else if (rss) expected.Add("rss");
            Check(calls.SequenceEqual(expected), $"startup flags connected={connected}, session={session}, feeds={feeds}, articles={articles}, rss={rss}, periodic={periodic}");
        }

        f = new Fixture(); f.Settings.NewsBlurConnected = true; f.Settings.EncryptedNewsBlurSession = "fake";
        var mirrorDelay = new TaskCompletionSource(); var refreshed = false;
        var startup = f.Window.RunStartupUpdatesAsync(() => mirrorDelay.Task, _ => { refreshed = true; return Task.CompletedTask; });
        Check(!refreshed, "startup article refresh waits for feed mirroring");
        Set(f.Window, "_closeInProgress", true); mirrorDelay.SetResult(); await startup;
        Check(!refreshed, "closing during startup mirroring does not start an article refresh");

        f = new Fixture(); f.Settings.NewsBlurConnected = true; f.Settings.EncryptedNewsBlurSession = "fake";
        var imported = false;
        await f.Window.RunStartupUpdatesAsync(() => { imported = true; return Task.CompletedTask; },
            newsBlur => { Check(imported && newsBlur, "a clean profile can refresh articles after its initial import"); return Task.CompletedTask; });

        var backupSettings = new AppSettings
        {
            CheckAppUpdatesAtStartup = false,
            SoundAlertsEnabled = false,
            SoundAlertOnSuccess = false,
            SoundAlertOnNewArticles = false,
            SoundAlertOnErrors = false,
            NewsBlurSyncFeedsAndFoldersAtStartup = false,
            NewsBlurSyncArticlesAndStatesAtStartup = false,
            NewsBlurAutoSyncEnabled = true,
            ReaderMode = ReaderModeIds.WebView,
            ArticleOpenMode = ArticleOpenModeIds.Orizont,
            EncryptedOpenAiKey = "synthetic-secret",
            OpenAiEnabled = true,
            NewsBlurConnected = true,
            EncryptedNewsBlurSession = "synthetic-session"
        };
        var safeBackup = BackupPolicy.SanitizeSettings(backupSettings);
        foreach (var property in new[] { "CheckAppUpdatesAtStartup", "SoundAlertsEnabled", "SoundAlertOnSuccess", "SoundAlertOnNewArticles", "SoundAlertOnErrors", "NewsBlurSyncFeedsAndFoldersAtStartup", "NewsBlurSyncArticlesAndStatesAtStartup", "NewsBlurAutoSyncEnabled", "ReaderMode", "ArticleOpenMode" })
            Check(Equals(typeof(AppSettings).GetProperty(property)!.GetValue(backupSettings), typeof(AppSettings).GetProperty(property)!.GetValue(safeBackup)), $"backup preserves {property}");
        Check(!safeBackup.OpenAiEnabled && safeBackup.EncryptedOpenAiKey is null, "backup continues to exclude API credentials");
        Check(!safeBackup.NewsBlurConnected && safeBackup.EncryptedNewsBlurSession is null, "backup continues to exclude NewsBlur session credentials");
        var serializedBackup = JsonSerializer.Serialize(safeBackup);
        var restoredSettings = JsonSerializer.Deserialize<AppSettings>(serializedBackup)!;
        foreach (var property in new[] { "CheckAppUpdatesAtStartup", "SoundAlertsEnabled", "SoundAlertOnSuccess", "SoundAlertOnNewArticles", "SoundAlertOnErrors", "NewsBlurSyncFeedsAndFoldersAtStartup", "NewsBlurSyncArticlesAndStatesAtStartup", "NewsBlurAutoSyncEnabled", "ReaderMode", "ArticleOpenMode" })
            Check(Equals(typeof(AppSettings).GetProperty(property)!.GetValue(safeBackup), typeof(AppSettings).GetProperty(property)!.GetValue(restoredSettings)), $"backup JSON round-trip preserves {property}");
        Check(!restoredSettings.OpenAiEnabled && restoredSettings.EncryptedOpenAiKey is null, "backup JSON round-trip excludes API credentials");
        Check(!restoredSettings.NewsBlurConnected && restoredSettings.EncryptedNewsBlurSession is null, "backup JSON round-trip excludes NewsBlur session credentials");

        f = new Fixture();
        f.Settings.Shortcuts = new Dictionary<string, string> { ["History"] = "Ctrl+F2" };
        f.Settings.ColorScheme = ColorThemeManager.BlackOnYellow;
        f.Settings.NewsBlurAutoSyncEnabled = true; f.Settings.NewsBlurConnected = true; f.Settings.EncryptedNewsBlurSession = "synthetic-session";
        Set(f.Window, "_startupSucceeded", true);
        typeof(MainWindow).GetMethod("ApplyRestoredRuntimeSettings", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(f.Window, null);
        Check(ShortcutBindings.Gesture("History") == "Ctrl+F2", "restored backup shortcuts take effect immediately");
        var restoredWindowColor = ((SolidColorBrush)Application.Current!.Resources["ThemeWindowBrush"]).Color;
        Check(restoredWindowColor.R == 0xFF && restoredWindowColor.G == 0xFF && restoredWindowColor.B == 0, "restored backup color scheme takes effect immediately");
        var restoreTimer = (DispatcherTimer?)typeof(MainWindow).GetField("_newsBlurAutoSyncTimer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(f.Window);
        Check(restoreTimer?.IsEnabled == true, "restored periodic sync preference reconfigures its timer");
        f.Settings.NewsBlurAutoSyncEnabled = false;
        typeof(MainWindow).GetMethod("ApplyRestoredRuntimeSettings", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(f.Window, null);
        Check(restoreTimer?.IsEnabled == false, "restoring disabled periodic sync stops its timer");

    }
}
