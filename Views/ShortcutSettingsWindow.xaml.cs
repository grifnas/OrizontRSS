using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class ShortcutSettingsWindow : Window
{
    private readonly AppSettings _settings;
    private bool _saving;
    private sealed record Row(ShortcutDefinition Definition, string Display)
    { public override string ToString() => Display; }
    public ShortcutSettingsWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();
        UiLocalizer.Apply(this);
        Refresh();
        Loaded += (_, _) => Search.Focus();
        Closing += (_, e) => { if (_saving) e.Cancel = true; };
    }
    private static string T(string text) => UiText.Translate(text);
    private ShortcutDefinition? Selected => (Commands.SelectedItem as Row)?.Definition;
    private void Search_Changed(object sender, TextChangedEventArgs e) { if (Commands is not null) Refresh(); }
    private void Refresh(string? selectedId = null)
    {
        var rows = ShortcutBindings.Definitions.Select(d => new Row(d,
            UiText.Format("{0}. Actuală: {1}. Implicită: {2}.", T(d.Name), ShortcutBindings.Display(d.Id, _settings.Shortcuts), d.DefaultGesture)))
            .Where(r => ShortcutSearch.Matches(r.Display, Search.Text)).ToList();
        Commands.ItemsSource = rows;
        Commands.SelectedIndex = rows.Count == 0 ? -1 : Math.Max(0, rows.FindIndex(r => r.Definition.Id == selectedId));
        ChangeButton.IsEnabled = rows.Count > 0;
        StatusAnnouncer.Set(Status, UiText.Format("Scurtături găsite: {0} din {1}.", rows.Count, ShortcutBindings.Definitions.Count));
    }
    private async void Change_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } d) return;
        var capture = new ShortcutCaptureWindow(d, _settings.Shortcuts) { Owner = this };
        if (capture.ShowDialog() != true) return;
        var candidate = new Dictionary<string, string>(_settings.Shortcuts) { [d.Id] = capture.Gesture };
        await SaveAsync(candidate, d.Id);
    }
    private async void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } d) return;
        await SaveAsync(new Dictionary<string, string>(_settings.Shortcuts) { [d.Id] = "" }, d.Id);
    }
    private async void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } d) return;
        var candidate = new Dictionary<string, string>(_settings.Shortcuts);
        candidate.Remove(d.Id);
        ShortcutBindings.TryParse(d.DefaultGesture, out var key, out var mods);
        var error = ShortcutBindings.Validate(d.Id, key, mods, candidate);
        if (error is not null) { MessageBox.Show(this, error, Title); return; }
        await SaveAsync(candidate, d.Id);
    }
    private async void ResetAll_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, T("Restabilești toate scurtăturile implicite?"), Title, MessageBoxButton.YesNo,
            MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes) await SaveAsync(new(), Selected?.Id);
    }
    private async Task SaveAsync(Dictionary<string, string> candidate, string? id)
    {
        if (_saving) return;
        _saving = true;
        ((UIElement)Content).IsEnabled = false;
        var old = _settings.Shortcuts;
        try
        {
            _settings.Shortcuts = candidate;
            await new FeedStore().SaveSettingsAsync(_settings);
            ShortcutBindings.Current = candidate;
            foreach (Window window in Application.Current.Windows) ShortcutBindings.RefreshMenuHints(window);
            Refresh(id);
            MessageBox.Show(this, T("Scurtăturile au fost salvate."), Title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _settings.Shortcuts = old;
            ShortcutBindings.Current = old;
            MessageBox.Show(this, UiText.Format("Operația nu a fost finalizată: {0}", ex.Message), Title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { _saving = false; ((UIElement)Content).IsEnabled = true; Commands.Focus(); }
    }
}

public sealed class ShortcutCaptureWindow : Window
{
    public string Gesture { get; private set; } = "";
    public ShortcutCaptureWindow(ShortcutDefinition definition, IReadOnlyDictionary<string, string> values)
    {
        Title = UiText.Translate("Schimbă scurtătura") + " — " + UiText.Translate(definition.Name);
        Width = 600; Height = 280; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(new TextBlock { Text = UiText.Translate("Apasă combinația în câmpul următor, apoi Tab și Salvează. Escape anulează. Conflictele cu JAWS sau Windows nu pot fi detectate complet."), TextWrapping = TextWrapping.Wrap });
        var capture = new TextBox { IsReadOnly = true, Margin = new Thickness(0, 12, 0, 12) };
        System.Windows.Automation.AutomationProperties.SetName(capture, UiText.Translate("Combinație nouă"));
        panel.Children.Add(capture);
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap };
        System.Windows.Automation.AutomationProperties.SetLiveSetting(status, System.Windows.Automation.AutomationLiveSetting.Polite);
        panel.Children.Add(status);
        var save = new Button { Content = UiText.Translate("Salvează"), IsEnabled = false };
        save.Click += (_, _) => DialogResult = true;
        var cancel = new Button { Content = UiText.Translate("Anulează"), IsCancel = true };
        panel.Children.Add(save); panel.Children.Add(cancel);
        capture.PreviewKeyDown += (_, e) =>
        {
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key is Key.Tab or Key.Escape) return;
            e.Handled = true;
            if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift) return;
            var error = ShortcutBindings.Validate(definition.Id, key, Keyboard.Modifiers, values);
            save.IsEnabled = error is null;
            Gesture = error is null ? ShortcutBindings.Format(key, Keyboard.Modifiers) : "";
            capture.Text = error ?? Gesture;
            StatusAnnouncer.Set(status, capture.Text);
            System.Windows.Automation.AutomationProperties.SetName(capture, UiText.Translate("Combinație nouă") + ": " + capture.Text);
        };
        Content = panel;
        Loaded += (_, _) => capture.Focus();
    }
}
