using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CititorRSS.Jaws;
using CititorRSS.Jaws.Localization;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        try { Run(); }
        catch (Exception ex) { Console.Error.WriteLine(ex); Environment.Exit(1); }
    }
    static void Run()
    {
        int count = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; }
        var app = new App(); app.InitializeComponent(); UiCulture.Apply("ro-RO");
        Check(!ShortcutBindings.Definitions.Any(d => d.Id is "Speak" or "Pause" or "Stop"), "Redundant speech shortcuts removed");
        Check(ShortcutBindings.Sanitize(new() { ["Speak"] = "Ctrl+Alt+V", ["Pause"] = "Ctrl+Alt+P", ["Stop"] = "Ctrl+Alt+S" }).Count == 0, "Legacy overrides cannot revive redundant speech shortcuts");
        var empty = new Dictionary<string, string>();
        ShortcutBindings.Current = empty;
        foreach (var d in ShortcutBindings.Definitions)
        {
            Check(ShortcutBindings.TryParse(d.DefaultGesture, out var key, out var mods), "parse " + d.Id);
            Check(ShortcutBindings.Validate(d.Id, key, mods, empty) is null, "default conflict " + d.Id);
            Check(ShortcutBindings.Matches(d.Id, key, mods), "default active " + d.Id);
        }
        Check(ShortcutBindings.Validate("Search", Key.F1, ModifierKeys.None, empty) is not null, "Duplicate blocked");
        foreach (var key in new[] { Key.Tab, Key.Escape, Key.F6, Key.F10, Key.Apps, Key.Delete, Key.Insert, Key.Enter })
            Check(ShortcutBindings.Validate("Search", key, ModifierKeys.None, empty) is not null, "Reserved " + key);
        Check(ShortcutBindings.Validate("Search", Key.C, ModifierKeys.Control, empty) is not null, "Copy protected");
        Check(ShortcutBindings.Validate("Search", Key.G, ModifierKeys.None, empty) is not null, "Typing protected");
        Check(ShortcutBindings.Validate("Read", Key.G, ModifierKeys.None, empty) is null, "List letter allowed");
        var custom = new Dictionary<string, string> { ["Feeds"] = "Ctrl+F2", ["Help"] = "Ctrl+Shift+F1", ["Favorite"] = "", ["Read"] = "G" };
        ShortcutBindings.Current = custom;
        Check(!ShortcutBindings.Matches("Help", Key.F1, ModifierKeys.None), "Old shortcut inactive");
        Check(ShortcutBindings.Matches("Help", Key.F1, ModifierKeys.Control | ModifierKeys.Shift), "New shortcut active");
        Check(!ShortcutBindings.Matches("Favorite", Key.F, ModifierKeys.None), "Removed shortcut inactive");
        Check(ShortcutBindings.IsArticleCommand(Key.G, ModifierKeys.None) && !ShortcutBindings.IsArticleCommand(Key.R, ModifierKeys.None), "Type ahead reserves actual letters");
        Check(!ShortcutBindings.Matches("VoiceToggle", Key.F9, ModifierKeys.Windows), "Windows modifier not ignored");
        Check(ShortcutBindings.Matches("Articles", Key.NumPad2, ModifierKeys.Control), "Numpad alias preserved");
        Check(ShortcutBindings.Sanitize(custom).Count == custom.Count, "Valid overrides survive normalization");
        Check(ShortcutBindings.Sanitize(new() { ["Help"] = "garbage", ["Feeds"] = "Ctrl+C" }).Count == 0, "Malformed or reserved overrides rejected");
        var temp = Path.Combine(Path.GetTempPath(), "orizont-shortcuts-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var save = typeof(FeedStore).GetMethod("SaveJsonAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(typeof(AppSettings));
            ((Task)save.Invoke(new FeedStore(), new object[] { temp, new AppSettings { Shortcuts = custom } })!).GetAwaiter().GetResult();
            var reloaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(temp))!;
            Check(reloaded.Shortcuts.OrderBy(x => x.Key).SequenceEqual(custom.OrderBy(x => x.Key)), "Settings round trip");
        }
        finally { File.Delete(temp); }
        var window = new ShortcutSettingsWindow(new AppSettings { Shortcuts = custom });
        var list = (ListBox)window.FindName("Commands");
        Check(list.Items.Count == 21, "All supported commands listed");
        list.ApplyTemplate(); list.Measure(new Size(700, 400)); list.Arrange(new Rect(0, 0, 700, 400)); list.UpdateLayout();
        var row = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(0);
        var name = System.Windows.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(row)?.GetName();
        Check(name?.Contains("Ctrl+Shift+F1") == true && !name.Contains("Definition"), "Accessible row excludes technical data");
        ((TextBox)window.FindName("Search")).Text = "export";
        Check(list.Items.Count == 1, "Search command");
        var menu = new Menu(); var item = new MenuItem { Header = "Help", InputGestureText = "F1" }; menu.Items.Add(item);
        ShortcutBindings.RefreshMenuHints(menu);
        Check(item.InputGestureText == "Ctrl+Shift+F1", "Menu updated");
        ShortcutBindings.Current = empty; ShortcutBindings.RefreshMenuHints(menu);
        Check(item.InputGestureText == "F1", "Menu restored");
        var main = new MainWindow();
        var dispatch = typeof(MainWindow).GetMethod("HandleConfiguredShortcut", BindingFlags.NonPublic | BindingFlags.Instance)!;
        ShortcutBindings.Current = custom;
        var evt = new KeyEventArgs(Keyboard.PrimaryDevice, new TestSource(), 0, Key.F2) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
        Check((bool)dispatch.Invoke(main, new object[] { Key.F2, ModifierKeys.Control, evt })! && evt.Handled, "Main routes reassigned panel key");
        evt = new KeyEventArgs(Keyboard.PrimaryDevice, new TestSource(), 0, Key.D1);
        Check(!(bool)dispatch.Invoke(main, new object[] { Key.D1, ModifierKeys.Control, evt })!, "Main does not route former key");
        evt = new KeyEventArgs(Keyboard.PrimaryDevice, new TestSource(), 0, Key.G);
        Check(!(bool)dispatch.Invoke(main, new object[] { Key.G, ModifierKeys.None, evt })!, "Article letters excluded from global handler");
        var dataPath = (string)typeof(FeedStore).GetField("SettingsPath", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        Check(dataPath == Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CititorRSS-JAWS", "settings.json"), "Stable profile path preserved");
        Console.WriteLine($"Shortcuts smoke passed: {count} checks. No windows shown or user data accessed.");
    }
    sealed class TestSource : PresentationSource
    {
        public override Visual RootVisual { get; set; } = null!;
        public override bool IsDisposed => false;
        protected override CompositionTarget GetCompositionTargetCore() => null!;
    }
}

