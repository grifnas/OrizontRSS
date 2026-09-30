using System.Reflection;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CititorRSS.Jaws;
using CititorRSS.Jaws.Localization;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        try { Run(); }
        catch (Exception exception) { Console.Error.WriteLine(exception); Environment.Exit(1); }
    }
    private static void Run()
    {
        var count = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); count++; }
        // No Application.Run/Show: no startup, real feeds, network or profile writes.
        var app = new App(); app.InitializeComponent();
        UiCulture.Apply("ro-RO");
        var a = new Article { Id = "fake", Title = "Android pentru toți", Tags = ["personal"] };
        var feed = new Feed { Name = "Fictiv", Articles = [a] };
        var rule = new ArticleRule { Name = "Mobil", Enabled = true, Terms = ["android"], Tags = ["mobil"], MarkReadLater = true };
        var editor = new ArticleRuleDialog(rule, [feed]);
        Check(((CheckBox)editor.FindName("EnabledBox")).IsChecked == true, "Editor activation");
        Check(!((ListBox)editor.FindName("FeedList")).IsEnabled, "All feeds disables selection");
        ((CheckBox)editor.FindName("AllFeedsBox")).IsChecked = false;
        Check(((ListBox)editor.FindName("FeedList")).IsEnabled, "Custom feed selection enabled");
        Check(editor.Rule != rule && editor.Rule.Terms != rule.Terms, "Editor edits a clone");
        var fresh = new ArticleRuleDialog(new(), [feed]);
        Check(((CheckBox)fresh.FindName("EnabledBox")).IsChecked == false, "New rule disabled");
        var manager = new ArticleRulesWindow([rule], [feed]);
        var ruleList = (ListBox)manager.FindName("RuleList");
        Check(ruleList.Items[0].ToString() == "Activată: Mobil", "Rule text excludes internal record/type names");
        ruleList.ApplyTemplate();
        ruleList.Measure(new Size(600, 350));
        ruleList.Arrange(new Rect(0, 0, 600, 350));
        ruleList.UpdateLayout();
        var ruleItem = (ListBoxItem)ruleList.ItemContainerGenerator.ContainerFromIndex(0);
        Check(ruleItem is not null && System.Windows.Automation.AutomationProperties.GetName(ruleItem) == "Activată: Mobil",
            "Rule container exposes only status and name");
        var rulePeer = System.Windows.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(ruleItem!);
        Check(rulePeer?.GetName() == "Activată: Mobil", "Automation peer name excludes technical details");
        manager.Rules[0].Enabled = false;
        Check(ruleList.Items[0].ToString() == "Dezactivată: Mobil", "Disabled rule text remains human-readable");
        Check(rule.Enabled, "Manager draft does not change saved rules");
        var temporaryRules = Path.Combine(Path.GetTempPath(), "orizont-rules-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var failSave = false;
            var persistedManager = new ArticleRulesWindow([], [feed], rules =>
            {
                if (failSave) return Task.FromException(new IOException("Simulated write failure"));
                File.WriteAllText(temporaryRules, System.Text.Json.JsonSerializer.Serialize(rules));
                return Task.CompletedTask;
            });
            var commit = typeof(ArticleRulesWindow).GetMethod("CommitRulesAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            void Save(List<ArticleRule> rules) => ((Task)commit.Invoke(persistedManager, [rules, 0])!).GetAwaiter().GetResult();
            List<ArticleRule> Reload() => System.Text.Json.JsonSerializer.Deserialize<List<ArticleRule>>(File.ReadAllText(temporaryRules))!;
            Save([rule]);
            Check(Reload().Single().Name == rule.Name, "One commit persists addition without closing manager");
            var edited = rule.Copy(); edited.Name = "Edited"; edited.Enabled = false;
            Save([edited]);
            Check(Reload().Single().Name == "Edited" && !Reload().Single().Enabled, "Edit and toggle persist immediately");
            var reopened = new ArticleRulesWindow(Reload(), [feed]);
            Check(reopened.Rules.Single().Name == "Edited", "Reopened manager keeps saved changes without outer save");
            failSave = true;
            try { Save([]); throw new Exception("Expected write failure"); }
            catch (IOException) { }
            Check(persistedManager.Rules.Count == 1 && Reload().Count == 1, "Failed write preserves UI and stored rules");
            failSave = false;
            Save([]);
            Check(Reload().Count == 0 && persistedManager.Rules.Count == 0, "Deletion persists immediately");
        }
        finally { if (File.Exists(temporaryRules)) File.Delete(temporaryRules); }
        var preview = new ArticleRulesPreviewWindow([rule], [feed], true);
        Check(preview.Changes.Count == 1 && preview.Changes[0].Tags.SequenceEqual(new[] { "mobil" }), "Preview action snapshot");
        Check(!a.ReadLater && a.Tags.SequenceEqual(new[] { "personal" }), "Preview does not mutate articles");
        Check(((ListBox)preview.FindName("Results")).Items.Count == 1, "Preview exposes selectable article rows");
        var resultList = (ListBox)preview.FindName("Results");
        Check(resultList.SelectedIndex == 0 && resultList.Items[0].ToString() == a.DisplayName, "Results default selection and readable text");
        resultList.ApplyTemplate();
        resultList.Measure(new Size(600, 350));
        resultList.Arrange(new Rect(0, 0, 600, 350));
        resultList.UpdateLayout();
        var resultItem = (ListBoxItem)resultList.ItemContainerGenerator.ContainerFromIndex(0);
        var resultPeer = System.Windows.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(resultItem);
        Check(resultPeer?.GetName() == a.DisplayName, "Result automation name is normal article text, not PreviewRow data");
        var prepareRequest = typeof(ArticleRulesPreviewWindow).GetMethod("PrepareArticleRequest", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Check((bool)prepareRequest.Invoke(preview, [true])! && ReferenceEquals(preview.RequestedArticle, a) && preview.RequestContextMenu,
            "Context request retains actual article identity");
        Check((bool)prepareRequest.Invoke(preview, [false])! && !preview.RequestContextMenu && !a.ReadLater,
            "Read request remains separate from applying rules");
        Check(((Button)preview.FindName("ApplyButton")).IsEnabled, "Apply available with results");
        var dryRun = new ArticleRulesPreviewWindow([rule], [feed], false);
        Check(((Button)dryRun.FindName("ApplyButton")).Visibility == Visibility.Collapsed, "Test cannot apply");
        var noResults = new ArticleRulesPreviewWindow([], [feed], true);
        Check(!((Button)noResults.FindName("ApplyButton")).IsEnabled, "Empty preview cannot apply");
        Check(!(bool)prepareRequest.Invoke(noResults, [true])!, "Empty result cannot request commands");

        var main = new MainWindow();
        var settingsField = typeof(MainWindow).GetField("_settings", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var baselineMethod = typeof(MainWindow).GetMethod("SetNewsBlurBaseline", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var remote = new NewsBlurStory("fake:1", "Android", "https://example.test/1", DateTimeOffset.UtcNow, false, false, ["remote"], "text");
        foreach (var mode in new[] { "ReadLater", "Both", "Favorites" })
        {
            settingsField.SetValue(main, new AppSettings { NewsBlurSavedStoryMode = mode });
            var imported = new Article { Title = "Android", Tags = ["remote"] };
            baselineMethod.Invoke(main, [imported, remote]);
            ArticleRules.Apply([rule], feed, imported);
            Check(imported.NewsBlurLastLocalSaved == false && imported.NewsBlurLastLocalTags!.SequenceEqual(new[] { "remote" }), "NewsBlur import baseline " + mode);
            var fromRss = new Article { Title = "Android" };
            ArticleRules.Apply([rule], feed, fromRss);
            Check(fromRss.AutomationPendingNewsBlurBaseline, "Pending initial baseline");
            baselineMethod.Invoke(main, [fromRss, remote]);
            Check(fromRss.NewsBlurLastLocalSaved == false && fromRss.ReadLater &&
                !fromRss.NewsBlurLastLocalTags!.Contains("mobil") && !fromRss.AutomationPendingNewsBlurBaseline, "RSS first association preserves local delta " + mode);
            Check(fromRss.Tags.Contains("remote"), "First association preserves remote tags");
            var tagsOnly = rule.Copy(); tagsOnly.MarkReadLater = false;
            var unsaved = new Article { Title = "Android" };
            ArticleRules.Apply([tagsOnly], feed, unsaved);
            baselineMethod.Invoke(main, [unsaved, remote with { IsStarred = true }]);
            Check(unsaved.NewsBlurLastLocalSaved == false && !unsaved.ReadLater && !unsaved.IsFavorite,
                "Tag-only rule does not create a local unstar delta");
        }
        var basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CititorRSS-JAWS");
        var path = typeof(FeedStore).GetField("FilePath", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null) as string;
        Check(path == Path.Combine(basePath, "feeds.json"), "Production data path preserved (checked without reading or writing)");
        Console.WriteLine($"Rules smoke passed: {count} checks. No windows shown; no user data or network accessed.");
        // Exit without closing MainWindow: its closing handler intentionally saves production data.
    }
}
