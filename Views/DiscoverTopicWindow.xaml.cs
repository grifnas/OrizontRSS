using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CititorRSS.Jaws.Localization;
using CititorRSS.Jaws.Models;
using CititorRSS.Jaws.Services.Rss;

namespace CititorRSS.Jaws;

public partial class DiscoverTopicWindow : Window
{
    private readonly RssReader _rss = new();
    public DiscoveredFeed? SelectedFeed { get; private set; }

    public DiscoverTopicWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PopulateCategories();
        PerformCatalogSearch();
        Keywords.Focus();
    }

    private void PopulateCategories()
    {
        CategoryFilter.Items.Clear();
        CategoryFilter.Items.Add(T("Toate categoriile"));

        var catalog = TopicFeedSearchService.GetCatalog();
        foreach (var category in catalog.Categories)
        {
            CategoryFilter.Items.Add(category.Name);
        }

        CategoryFilter.SelectedIndex = 0;
    }

    private void PerformCatalogSearch()
    {
        var selectedCategory = CategoryFilter.SelectedItem as string;
        var query = Keywords.Text.Trim();

        var catalogItems = TopicFeedSearchService.SearchCatalog(query, selectedCategory);
        var discoveredList = catalogItems.Select(item => new DiscoveredFeed
        {
            Name = item.Name,
            Url = item.Url,
            SourceSite = item.Category,
            RequiresVerification = true,
            IsVerified = false,
            ArticleCount = 0
        }).ToList();

        Results.ItemsSource = discoveredList;
        AddButton.IsEnabled = false;

        if (discoveredList.Count == 0)
        {
            StatusText.Text = T("Nu există rubrici în catalog pentru filtrul curent. Apasă 'Caută pe internet' pentru a căuta live.");
        }
        else
        {
            StatusText.Text = F("{0} feeduri găsite în catalog. Selectează unul și verifică-l.", discoveredList.Count);
        }
    }

    private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded) PerformCatalogSearch();
    }

    private void SearchCatalog_Click(object sender, RoutedEventArgs e)
    {
        PerformCatalogSearch();
    }

    private async void LiveSearch_Click(object sender, RoutedEventArgs e)
    {
        var query = Keywords.Text.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            StatusText.Text = T("Tastează un cuvânt-cheie pentru căutarea pe internet.");
            return;
        }

        StatusText.Text = F("Se caută pe internet pentru '{0}'...", query);
        LiveSearchButton.IsEnabled = false;

        try
        {
            var liveItems = await TopicFeedSearchService.SearchLiveAsync(query);
            var discoveredLive = liveItems.Select(item => new DiscoveredFeed
            {
                Name = item.Name,
                Url = item.Url,
                SourceSite = item.Category,
                RequiresVerification = true,
                IsVerified = false,
                ArticleCount = 0
            }).ToList();

            if (discoveredLive.Count == 0)
            {
                StatusText.Text = F("Nu s-au găsit feeduri noi pe internet pentru '{0}'.", query);
            }
            else
            {
                Results.ItemsSource = discoveredLive;
                StatusText.Text = F("{0} feeduri live găsite pe internet. Selectează unul și verifică-l.", discoveredLive.Count);
            }
        }
        finally
        {
            LiveSearchButton.IsEnabled = true;
        }
    }

    private async void Verify_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedFeed is null)
        {
            StatusText.Text = T("Selectează un feed din listă.");
            return;
        }

        try
        {
            StatusText.Text = F("Se verifică conexiunea la {0}...", SelectedFeed.Name);
            var articles = await _rss.LoadAsync(SelectedFeed.Url);
            SelectedFeed.IsVerified = true;
            SelectedFeed.ArticleCount = articles.Count;
            Results.Items.Refresh();
            AddButton.IsEnabled = true;
            StatusText.Text = F("Feed verificat cu succes: {0} articole disponibile.", articles.Count);
        }
        catch (Exception exception)
        {
            SelectedFeed.IsVerified = false;
            AddButton.IsEnabled = false;
            StatusText.Text = F("Feed nefuncțional sau indisponibil: {0}", exception.Message);
        }
    }

    private void Results_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedFeed = Results.SelectedItem as DiscoveredFeed;
        AddButton.IsEnabled = SelectedFeed?.IsVerified == true;
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedFeed?.IsVerified == true) DialogResult = true;
    }

    private void Keywords_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            PerformCatalogSearch();
            e.Handled = true;
        }
    }

    private static string T(string source) => UiText.Translate(source);
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);
}
