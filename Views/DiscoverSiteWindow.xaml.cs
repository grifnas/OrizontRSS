using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class DiscoverSiteWindow : Window
{
    private readonly RssReader _rss = new();
    public DiscoveredFeed? SelectedFeed { get; private set; }
    public DiscoverSiteWindow() { InitializeComponent(); Loaded += (_, _) => SiteAddress.Focus(); }
    private async void Discover_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = T("Se caută feedurile declarate de site.");
            var found = await _rss.DiscoverAsync(SiteAddress.Text.Trim());
            FoundFeeds.ItemsSource = found;
            StatusText.Text = found.Count == 0 ? T("Nu au fost găsite adrese posibile.") : F("{0} adrese posibile găsite. Sunt incluse feedurile declarate de site și adrese standard; selectează una și verific-o.", found.Count);
            AddButton.IsEnabled = false;
        }
        catch (Exception exception) { StatusText.Text = F("Nu s-a putut analiza site-ul: {0}", exception.Message); }
    }
    private async void Verify_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedFeed is null) { StatusText.Text = T("Selectează un feed."); return; }
        try
        {
            StatusText.Text = F("Se verifică {0}.", SelectedFeed.Name);
            var articles = await _rss.LoadAsync(SelectedFeed.Url);
            SelectedFeed.IsVerified = true; SelectedFeed.ArticleCount = articles.Count;
            FoundFeeds.Items.Refresh(); AddButton.IsEnabled = true;
            StatusText.Text = F("Feed verificat: {0} articole disponibile.", articles.Count);
        }
        catch (Exception exception) { SelectedFeed.IsVerified = false; AddButton.IsEnabled = false; StatusText.Text = F("Feed nefuncțional: {0}", exception.Message); }
    }
    private void FoundFeeds_SelectionChanged(object sender, SelectionChangedEventArgs e) { SelectedFeed = FoundFeeds.SelectedItem as DiscoveredFeed; AddButton.IsEnabled = SelectedFeed?.IsVerified == true; }
    private void Add_Click(object sender, RoutedEventArgs e) { if (SelectedFeed?.IsVerified == true) DialogResult = true; }
    private void SiteAddress_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) { Discover_Click(sender, e); e.Handled = true; } }
    private static string T(string source) => UiText.Translate(source);
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);
}
