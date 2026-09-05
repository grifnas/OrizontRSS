using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class DiscoverTopicWindow : Window
{
    private readonly FeedCatalog _catalog = new();
    private readonly RssReader _rss = new();
    public DiscoveredFeed? SelectedFeed { get; private set; }
    public DiscoverTopicWindow() { InitializeComponent(); Loaded += (_, _) => Keywords.Focus(); }
    private void Search_Click(object sender, RoutedEventArgs e)
    {
        var results = _catalog.Search(Keywords.Text);
        Results.ItemsSource = results;
        AddButton.IsEnabled = false;
        StatusText.Text = results.Count == 0 ? T("Nu există rubrici concrete potrivite în catalog. Încearcă un termen mai general.") : F("{0} feeduri concrete găsite. Selectează unul și verifică-l.", results.Count);
    }
    private async void Verify_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedFeed is null) { StatusText.Text = T("Selectează un feed."); return; }
        try { StatusText.Text = F("Se verifică {0}.", SelectedFeed.Name); var articles = await _rss.LoadAsync(SelectedFeed.Url); SelectedFeed.IsVerified = true; SelectedFeed.ArticleCount = articles.Count; Results.Items.Refresh(); AddButton.IsEnabled = true; StatusText.Text = F("Feed verificat: {0} articole disponibile.", articles.Count); }
        catch (Exception exception) { SelectedFeed.IsVerified = false; AddButton.IsEnabled = false; StatusText.Text = F("Feed nefuncțional: {0}", exception.Message); }
    }
    private void Results_SelectionChanged(object sender, SelectionChangedEventArgs e) { SelectedFeed = Results.SelectedItem as DiscoveredFeed; AddButton.IsEnabled = SelectedFeed?.IsVerified == true; }
    private void Add_Click(object sender, RoutedEventArgs e) { if (SelectedFeed?.IsVerified == true) DialogResult = true; }
    private void Keywords_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) { Search_Click(sender, e); e.Handled = true; } }
    private static string T(string source) => UiText.Translate(source);
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);
}
