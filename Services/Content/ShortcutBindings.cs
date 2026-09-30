using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public sealed record ShortcutDefinition(string Id, string Name, string DefaultGesture, string Scope = "Main");

public static class ShortcutBindings
{
    public static IReadOnlyList<ShortcutDefinition> Definitions { get; } = new List<ShortcutDefinition>
    {
        new("Help", "Ajutor și scurtături", "F1"),
        new("Window", "Maximizează sau restabilește fereastra.", "F11", "Shared"),
        new("VoiceSettings", "Setări voce", "Shift+F9"),
        new("VoiceToggle", "Citire vocală: pornește, întrerupe sau continuă", "F9", "Shared"),
        new("Feeds", "Mută focalizarea în panoul Feeduri.", "Ctrl+1"),
        new("Articles", "Mută focalizarea în lista Articole.", "Ctrl+2"),
        new("Content", "Mută focalizarea în Conținut articol.", "Ctrl+3"),
        new("ReadNow", "Citește acum", "Ctrl+4"),
        new("Folders", "Mută focalizarea direct în selectorul de foldere.", "Ctrl+Shift+1"),
        new("FolderArticles", "Afișează lista combinată a articolelor din folderul selectat și mută focalizarea în ea.", "Ctrl+Shift+2"),
        new("Export", "Salvează articolul curent în formatul implicit din folderul de export.", "Ctrl+Shift+S"),
        new("History", "Istoric stare și erori", "Ctrl+Shift+H"),
        new("Filters", "Filtre articole", "Ctrl+Shift+F"),
        new("Unread", "Toate articolele necitite", "Ctrl+Shift+U"),
        new("FullText", "Adu textul complet de pe site", "Ctrl+Shift+R"),
        new("Search", "Caută în articole", "Ctrl+F"),
        new("SearchAlternate", "Caută în articole — alternativă", "F3"),
        new("Read", "Marchează articolele selectate ca citite sau necitite.", "R", "Articles"),
        new("Favorite", "Adaugă sau elimină articolele selectate din Favorite.", "F", "Articles"),
        new("Later", "Adaugă sau elimină articolele selectate din De citit mai târziu.", "L", "Articles"),
        new("ReaderMode", "Comută între Text și WebReader în fereastra Cititor Orizont.", "Ctrl+Shift+F8", "Reader")
    };
    public static IReadOnlyDictionary<string, string> Current { get; set; } = new Dictionary<string, string>();
    public static string Gesture(string id, IReadOnlyDictionary<string, string>? values = null)
    {
        values ??= Current;
        return values.TryGetValue(id, out var value) ? value : Definitions.First(d => d.Id == id).DefaultGesture;
    }
    public static string Display(string id, IReadOnlyDictionary<string, string>? values = null)
    {
        var value = Gesture(id, values);
        return value.Length == 0 ? UiText.Translate("Fără scurtătură") : value;
    }
    public static Key Normalize(Key key) => key >= Key.NumPad0 && key <= Key.NumPad9 ? Key.D0 + (key - Key.NumPad0) : key;
    public static string Format(Key key, ModifierKeys mods)
    {
        key = Normalize(key);
        var name = key >= Key.D0 && key <= Key.D9 ? ((int)(key - Key.D0)).ToString() : key.ToString();
        return (mods.HasFlag(ModifierKeys.Control) ? "Ctrl+" : "") + (mods.HasFlag(ModifierKeys.Alt) ? "Alt+" : "") +
            (mods.HasFlag(ModifierKeys.Shift) ? "Shift+" : "") + name;
    }
    public static bool Matches(string id, Key key, ModifierKeys mods) => !mods.HasFlag(ModifierKeys.Windows) && Gesture(id).Length > 0 && Gesture(id) == Format(key, mods);
    public static bool IsArticleCommand(Key key, ModifierKeys mods) => new[] { "Read", "Favorite", "Later" }.Any(id => Matches(id, key, mods));
    public static string? Validate(string id, Key key, ModifierKeys mods, IReadOnlyDictionary<string, string> values)
    {
        key = Normalize(key);
        var definition = Definitions.First(d => d.Id == id);
        if (mods.HasFlag(ModifierKeys.Windows) || key is Key.None or Key.System or Key.ImeProcessed or Key.DeadCharProcessed
            or Key.Tab or Key.Enter or Key.Escape or Key.Space or Key.Delete or Key.Back or Key.Insert or Key.Apps
            or Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End or Key.PageUp or Key.PageDown
            or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin
            || key == Key.F6 || key == Key.F10 || (key == Key.F4 && mods.HasFlag(ModifierKeys.Alt))
            || (mods == ModifierKeys.Control && key is Key.A or Key.C or Key.V or Key.X or Key.Z or Key.Y)
            || mods == ModifierKeys.Alt || mods == (ModifierKeys.Control | ModifierKeys.Alt) && key == Key.Delete)
            return UiText.Translate("Combinație rezervată pentru navigare sau sistem.");
        if ((mods == ModifierKeys.None || mods == ModifierKeys.Shift) && !(key >= Key.F1 && key <= Key.F24)
            && !(definition.Scope == "Articles" && mods == ModifierKeys.None && key >= Key.A && key <= Key.Z))
            return UiText.Translate("Folosește Ctrl sau Ctrl+Shift împreună cu tasta aleasă.");
        var gesture = Format(key, mods);
        // Conservative: no duplicates even across windows, so a future shared handler cannot shadow another command.
        var conflict = Definitions.FirstOrDefault(d => d.Id != id && Gesture(d.Id, values) == gesture);
        return conflict is null ? null : UiText.Format("Combinația este folosită de: {0}.", UiText.Translate(conflict.Name));
    }
    public static Dictionary<string, string> Sanitize(Dictionary<string, string>? values)
    {
        var result = new Dictionary<string, string>();
        if (values is null) return result;
        // Start with all syntactically valid overrides to allow valid swaps of default keys.
        foreach (var d in Definitions)
            if (values.TryGetValue(d.Id, out var g) && (g == "" || TryParse(g, out _, out _))) result[d.Id] = g;
        for (var pass = 0; pass < Definitions.Count; pass++)
        {
            var changed = false;
            foreach (var d in Definitions)
                if (result.TryGetValue(d.Id, out var g) && g != "" && TryParse(g, out var k, out var m) && Validate(d.Id, k, m, result) is not null)
                { result.Remove(d.Id); changed = true; }
            if (!changed) break;
        }
        return result;
    }
    public static bool TryParse(string text, out Key key, out ModifierKeys mods)
    {
        key = Key.None; mods = ModifierKeys.None;
        var parts = text.Split('+');
        foreach (var part in parts.SkipLast(1))
        {
            if (part == "Ctrl") mods |= ModifierKeys.Control;
            else if (part == "Alt") mods |= ModifierKeys.Alt;
            else if (part == "Shift") mods |= ModifierKeys.Shift;
            else return false;
        }
        var last = parts.Last();
        if (last.Length == 1 && char.IsAsciiDigit(last[0])) last = "D" + last;
        return Enum.TryParse(last, out key) && Enum.IsDefined(key) && Format(key, mods) == text;
    }
    public static void RefreshMenuHints(DependencyObject root)
    {
        if (root is FrameworkElement helpElement)
        {
            var help = helpElement.GetValue(OriginalHelpProperty) as string;
            if (help is null)
            {
                help = System.Windows.Automation.AutomationProperties.GetHelpText(helpElement);
                helpElement.SetValue(OriginalHelpProperty, help);
            }
            var definitions = Definitions.Where(d => d.DefaultGesture.Length > 1).OrderByDescending(d => d.DefaultGesture.Length).ToList();
            var pattern = @"(?<![A-Za-z0-9+])(?:" + string.Join("|", definitions.Select(d => System.Text.RegularExpressions.Regex.Escape(d.DefaultGesture))) + @")(?![A-Za-z0-9+])";
            System.Windows.Automation.AutomationProperties.SetHelpText(helpElement,
                System.Text.RegularExpressions.Regex.Replace(help, pattern, m => Display(definitions.First(d => d.DefaultGesture == m.Value).Id)));
        }
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            if (child is MenuItem item)
            {
                var original = item.GetValue(OriginalHintProperty) as string;
                if (original is null) { original = item.InputGestureText; item.SetValue(OriginalHintProperty, original); }
                item.InputGestureText = TranslateHint(original);
            }
            RefreshMenuHints(child);
        }
        if (root is FrameworkElement element && element.ContextMenu is { } menu) RefreshMenuHints(menu);
    }
    private static readonly DependencyProperty OriginalHintProperty = DependencyProperty.RegisterAttached("OriginalHint", typeof(string), typeof(ShortcutBindings));
    private static readonly DependencyProperty OriginalHelpProperty = DependencyProperty.RegisterAttached("OriginalHelp", typeof(string), typeof(ShortcutBindings));
    public static string TranslateHint(string original)
    {
        if (original == "Ctrl+F / F3" || original == "Ctrl+F sau F3") return Display("Search") + " / " + Display("SearchAlternate");
        var d = Definitions.FirstOrDefault(d => d.DefaultGesture == original);
        return d is null ? original : Display(d.Id);
    }
}
