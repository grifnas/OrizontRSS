using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class DiscoverSiteWindow : Window
{
    private readonly RssReader _rss = new();
    private readonly FeedDiscoveryService _discovery = new();
    private CancellationTokenSource? _discoveryCancellation;
    public DiscoveredFeed? SelectedFeed { get; private set; }
    public DiscoverSiteWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => SiteAddress.Focus();
        Closed += (_, _) => _discoveryCancellation?.Cancel();
    }
    private async void Discover_Click(object sender, RoutedEventArgs e)
    {
        if (_discoveryCancellation is not null) return;
        var cancellation = new CancellationTokenSource();
        _discoveryCancellation = cancellation;
        DiscoverButton.IsEnabled = false;
        AddButton.IsEnabled = false;
        try
        {
            StatusText.Text = T("Se caută și se verifică feedurile site-ului.");
            var found = await _discovery.DiscoverAsync(SiteAddress.Text.Trim(), cancellation.Token);
            FoundFeeds.ItemsSource = found;
            if (found.Count == 0)
            {
                StatusText.Text = T("Nu a fost găsit niciun feed funcțional pentru această adresă.");
                return;
            }
            StatusText.Text = F("{0} feeduri funcționale găsite și verificate. Alege unul și apasă Adaugă feed verificat.", found.Count);
            // Rezultatele sunt deja verificate: primul este selectat, iar JAWS îl citește odată cu mutarea focalizării.
            FoundFeeds.SelectedIndex = 0;
            FoundFeeds.UpdateLayout();
            (FoundFeeds.ItemContainerGenerator.ContainerFromIndex(0) as UIElement)?.Focus();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception exception) { StatusText.Text = F("Nu s-a putut analiza site-ul: {0}", exception.Message); }
        finally
        {
            _discoveryCancellation = null;
            cancellation.Dispose();
            DiscoverButton.IsEnabled = true;
        }
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
