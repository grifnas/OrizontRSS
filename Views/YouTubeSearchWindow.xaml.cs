using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CititorRSS.Jaws.Localization;
using CititorRSS.Jaws.Models;
using CititorRSS.Jaws.Services.Rss;

namespace CititorRSS.Jaws;

public partial class YouTubeSearchWindow : Window
{
    private const int TypeChannelsIndex = 0;
    private const int TypePlaylistsIndex = 1;
    private const int TypeVideosIndex = 2;

    private CancellationTokenSource? _searchCancellation;
    private string _videoQuery = string.Empty;
    private string _videoContinuation = string.Empty;
    private ObservableCollection<YouTubeVideoSearchService.YouTubeVideoCandidate>? _videoResults;
    private readonly HashSet<string> _videoIds = new(StringComparer.Ordinal);
    public DiscoveredFeed? SelectedFeed { get; private set; }

    public YouTubeSearchWindow(string initialQuery = "")
    {
        InitializeComponent();
        Keywords.Text = initialQuery;
        PopulateSearchTypes();
        UpdateSearchTypeActions();
        WindowStatusAnnouncer.Attach(this, () => UiText.Translate("Caută pe YouTube") + " " + UiText.Translate("Folosește Tab pentru a parcurge comenzile."));
        Loaded += (_, _) => Keywords.Focus();
        Closed += (_, _) => _searchCancellation?.Cancel();
    }

    private void PopulateSearchTypes()
    {
        SearchType.Items.Clear();
        SearchType.Items.Add(T("Canale (pentru abonare RSS)"));
        SearchType.Items.Add(T("Playlisturi (pentru abonare RSS)"));
        SearchType.Items.Add(T("Videoclipuri (pentru vizionare în browser)"));
        SearchType.SelectedIndex = TypeChannelsIndex;
    }

    private void SearchType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        Results.ItemsSource = null;
        ResetVideoPaging();
        StatusText.Text = string.Empty;
        UpdateSearchTypeActions();
        UpdateActionButtons();
    }

    private void UpdateSearchTypeActions()
    {
        var isVideo = SearchType.SelectedIndex == TypeVideosIndex;
        var isFeedSearch = !isVideo;
        AddFeedButton.Visibility = isFeedSearch ? Visibility.Visible : Visibility.Collapsed;
        VerifyFeedButton.Visibility = isFeedSearch ? Visibility.Visible : Visibility.Collapsed;
        OpenResultPageButton.Visibility = isFeedSearch ? Visibility.Visible : Visibility.Collapsed;
        OpenVideoButton.Visibility = isVideo ? Visibility.Visible : Visibility.Collapsed;
        OpenInAppButton.Visibility = isVideo ? Visibility.Visible : Visibility.Collapsed;
        LoadMoreButton.Visibility = isVideo ? Visibility.Visible : Visibility.Collapsed;
        SubscribeChannelButton.Visibility = isVideo ? Visibility.Visible : Visibility.Collapsed;
        ImportTranscriptButton.Visibility = isVideo ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SearchType_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        Search_Click(SearchButton, new RoutedEventArgs());
        e.Handled = true;
    }

    private async void Search_Click(object sender, RoutedEventArgs e)
    {
        var query = Keywords.Text.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            StatusText.Text = T("Tastează un cuvânt-cheie pentru căutarea pe YouTube.");
            Keywords.Focus();
            return;
        }

        if (_searchCancellation is not null) return;
        var cancellation = new CancellationTokenSource();
        _searchCancellation = cancellation;
        SetControlsBusy(true);
        Results.ItemsSource = null;
        ResetVideoPaging();

        try
        {
            var language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (SearchType.SelectedIndex == TypeChannelsIndex)
            {
                StatusText.Text = T("Se caută canale YouTube. Cuvintele căutate sunt trimise la YouTube.");
                var found = await YouTubeChannelSearchService.SearchAsync(query, language, cancellation.Token);
                if (found.Count == 0)
                {
                    StatusText.Text = F("Nu s-au găsit canale YouTube cu flux RSS funcțional pentru „{0}”.", query);
                    return;
                }
                Results.ItemsSource = found;
                StatusText.Text = F("{0} rezultate găsite. Fluxuri RSS verificate: {1}; neverificate: {2}.",
                    found.Count, found.Count(feed => feed.IsVerified), found.Count(feed => !feed.IsVerified));
            }
            else if (SearchType.SelectedIndex == TypePlaylistsIndex)
            {
                StatusText.Text = T("Se caută playlisturi YouTube. Cuvintele căutate sunt trimise la YouTube.");
                var found = await YouTubePlaylistSearchService.SearchAsync(query, language, cancellation.Token);
                if (found.Count == 0)
                {
                    StatusText.Text = F("Nu s-au găsit playlisturi YouTube cu flux RSS funcțional pentru „{0}”.", query);
                    return;
                }
                Results.ItemsSource = found;
                StatusText.Text = F("{0} rezultate găsite. Fluxuri RSS verificate: {1}; neverificate: {2}.",
                    found.Count, found.Count(feed => feed.IsVerified), found.Count(feed => !feed.IsVerified));
            }
            else
            {
                StatusText.Text = T("Se caută videoclipuri YouTube. Termenii sunt trimiși la YouTube.");
                var page = await YouTubeVideoSearchService.SearchPageAsync(query, language, string.Empty, cancellation.Token);
                if (page.Videos.Count == 0)
                {
                    StatusText.Text = F("Nu s-au găsit videoclipuri YouTube pentru „{0}”.", query);
                    return;
                }
                _videoQuery = query;
                _videoContinuation = page.ContinuationToken;
                _videoResults = new ObservableCollection<YouTubeVideoSearchService.YouTubeVideoCandidate>();
                foreach (var video in page.Videos)
                    if (_videoIds.Add(video.VideoId))
                        _videoResults.Add(video);
                Results.ItemsSource = _videoResults;
                StatusText.Text = _videoContinuation.Length > 0
                    ? F("{0} videoclipuri YouTube găsite. Primul rezultat este selectat. Pentru mai multe rezultate folosește butonul Mai multe rezultate.", _videoResults.Count)
                    : F("{0} videoclipuri YouTube găsite. Primul rezultat este selectat.", _videoResults.Count);
            }

            Results.SelectedIndex = 0;
            Results.UpdateLayout();
            (Results.ItemContainerGenerator.ContainerFromIndex(0) as UIElement)?.Focus();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception exception)
        {
            Results.ItemsSource = null;
            StatusText.Text = F("Căutarea pe YouTube nu a reușit: {0}", exception.Message);
        }
        finally
        {
            _searchCancellation = null;
            cancellation.Dispose();
            SetControlsBusy(false);
            UpdateActionButtons();
        }
    }

    private void ResetVideoPaging()
    {
        _videoQuery = string.Empty;
        _videoContinuation = string.Empty;
        _videoResults = null;
        _videoIds.Clear();
    }

    private async void LoadMore_Click(object sender, RoutedEventArgs e)
    {
        if (_videoResults is null || _videoContinuation.Length == 0) return;
        if (_searchCancellation is not null) return;

        var cancellation = new CancellationTokenSource();
        _searchCancellation = cancellation;
        SetControlsBusy(true);
        StatusText.Text = T("Se încarcă mai multe videoclipuri YouTube...");

        try
        {
            var language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var page = await YouTubeVideoSearchService.SearchPageAsync(_videoQuery, language, _videoContinuation, cancellation.Token);
            _videoContinuation = page.ContinuationToken;

            var firstNewIndex = _videoResults.Count;
            foreach (var video in page.Videos)
                if (_videoIds.Add(video.VideoId))
                    _videoResults.Add(video);
            var added = _videoResults.Count - firstNewIndex;

            if (added == 0)
            {
                StatusText.Text = T("Nu mai sunt alte rezultate YouTube pentru această căutare.");
                return;
            }

            StatusText.Text = F("{0} videoclipuri noi adăugate; în total {1}. Primul rezultat nou este selectat.", added, _videoResults.Count);
            Results.SelectedIndex = firstNewIndex;
            Results.UpdateLayout();
            Results.ScrollIntoView(_videoResults[firstNewIndex]);
            (Results.ItemContainerGenerator.ContainerFromIndex(firstNewIndex) as UIElement)?.Focus();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception exception)
        {
            StatusText.Text = F("Căutarea pe YouTube nu a reușit: {0}", exception.Message);
        }
        finally
        {
            _searchCancellation = null;
            cancellation.Dispose();
            SetControlsBusy(false);
            UpdateActionButtons();
        }
    }

    private void Results_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdateActionButtons();

    private void UpdateActionButtons()
    {
        if (SearchType.SelectedIndex == TypeVideosIndex)
        {
            var isVideo = Results.SelectedItem is YouTubeVideoSearchService.YouTubeVideoCandidate;
            OpenVideoButton.IsEnabled = isVideo;
            OpenInAppButton.IsEnabled = isVideo;
            SubscribeChannelButton.IsEnabled = isVideo;
            ImportTranscriptButton.IsEnabled = isVideo;
            LoadMoreButton.IsEnabled = _videoContinuation.Length > 0;
            AddFeedButton.IsEnabled = false;
        }
        else
        {
            var feed = Results.SelectedItem as DiscoveredFeed;
            AddFeedButton.IsEnabled = feed?.IsVerified == true;
            VerifyFeedButton.Visibility = feed is not null && !feed.IsVerified ? Visibility.Visible : Visibility.Collapsed;
            VerifyFeedButton.IsEnabled = feed is not null && !feed.IsVerified;
            OpenResultPageButton.Visibility = string.IsNullOrWhiteSpace(feed?.WebPageUrl) ? Visibility.Collapsed : Visibility.Visible;
            OpenResultPageButton.IsEnabled = !string.IsNullOrWhiteSpace(feed?.WebPageUrl);
            OpenVideoButton.IsEnabled = false;
            OpenInAppButton.IsEnabled = false;
            SubscribeChannelButton.IsEnabled = false;
            ImportTranscriptButton.IsEnabled = false;
            LoadMoreButton.IsEnabled = false;
        }
    }

    private async void VerifyFeed_Click(object sender, RoutedEventArgs e)
    {
        if (Results.SelectedItem is not DiscoveredFeed feed) return;
        if (_searchCancellation is not null) return;

        var cancellation = new CancellationTokenSource();
        _searchCancellation = cancellation;
        SetControlsBusy(true);
        StatusText.Text = F("Se verifică conexiunea la {0}...", feed.Name);

        try
        {
            var preview = await new RssReader().LoadPreviewAsync(feed.Url, cancellation.Token);
            feed.Url = preview.Url;
            feed.IsVerified = true;
            feed.ArticleCount = preview.Articles.Count;
            feed.LatestArticle = preview.Articles.Count == 0
                ? null
                : preview.Articles.Max(article => article.Published);
            Results.Items.Refresh();
            StatusText.Text = F("Feed verificat cu succes: {0} articole disponibile.", feed.ArticleCount);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception exception)
        {
            feed.IsVerified = false;
            Results.Items.Refresh();
            StatusText.Text = F("Feed nefuncțional sau indisponibil: {0}", exception.Message);
        }
        finally
        {
            _searchCancellation = null;
            cancellation.Dispose();
            SetControlsBusy(false);
            UpdateActionButtons();
        }
    }

    private void OpenResultPage_Click(object sender, RoutedEventArgs e)
    {
        if (Results.SelectedItem is not DiscoveredFeed feed || string.IsNullOrWhiteSpace(feed.WebPageUrl)) return;
        try
        {
            Process.Start(new ProcessStartInfo(feed.WebPageUrl) { UseShellExecute = true });
            StatusText.Text = T("Pagina YouTube se deschide în browserul implicit.");
        }
        catch (Exception exception)
        {
            StatusText.Text = F("Pagina YouTube nu s-a putut deschide în browser: {0}", exception.Message);
        }
    }

    private void AddFeed_Click(object sender, RoutedEventArgs e)
    {
        if (Results.SelectedItem is DiscoveredFeed feed && feed.IsVerified)
        {
            SelectedFeed = feed;
            DialogResult = true;
        }
    }

    private void OpenInApp_Click(object sender, RoutedEventArgs e)
    {
        if (Results.SelectedItem is not YouTubeVideoSearchService.YouTubeVideoCandidate video) return;
        try
        {
            var player = new YouTubePlayerWindow(video.VideoId, video.Title) { Owner = this };
            player.Show();
            StatusText.Text = T("Videoclipul se deschide în Orizont.");
        }
        catch (Exception exception)
        {
            StatusText.Text = F("Nu s-a putut deschide videoclipul în Orizont: {0}", exception.Message);
        }
    }

    private void OpenVideo_Click(object sender, RoutedEventArgs e)
    {
        if (Results.SelectedItem is not YouTubeVideoSearchService.YouTubeVideoCandidate video) return;
        try
        {
            Process.Start(new ProcessStartInfo(video.Url) { UseShellExecute = true });
            StatusText.Text = T("Videoclipul se deschide în browserul implicit.");
        }
        catch (Exception exception)
        {
            StatusText.Text = F("Nu s-a putut deschide videoclipul în browser: {0}", exception.Message);
        }
    }

    private async void SubscribeChannel_Click(object sender, RoutedEventArgs e)
    {
        if (Results.SelectedItem is not YouTubeVideoSearchService.YouTubeVideoCandidate video) return;
        if (string.IsNullOrWhiteSpace(video.ChannelName) && string.IsNullOrWhiteSpace(video.ChannelId))
        {
            StatusText.Text = T("Videoclipul selectat nu conține un canal asociat.");
            return;
        }

        if (_searchCancellation is not null) return;
        var cancellation = new CancellationTokenSource();
        _searchCancellation = cancellation;
        SetControlsBusy(true);

        try
        {
            if (!string.IsNullOrWhiteSpace(video.ChannelId))
            {
                var feedUrl = YouTubeChannelSearchService.BuildFeedUrl(video.ChannelId);
                StatusText.Text = F("Se verifică fluxul canalului „{0}”...", video.ChannelName);
                var reader = new RssReader();
                var preview = await reader.LoadPreviewAsync(feedUrl, cancellation.Token);
                if (preview.Articles.Count > 0)
                {
                    SelectedFeed = new DiscoveredFeed
                    {
                        Name = string.IsNullOrWhiteSpace(video.ChannelName) ? preview.Title : video.ChannelName,
                        Url = preview.Url,
                        WebPageUrl = string.IsNullOrWhiteSpace(video.ChannelId)
                            ? string.Empty
                            : YouTubeChannelSearchService.BuildChannelUrl(video.ChannelId),
                        SourceSite = "YouTube",
                        IsVerified = true,
                        ArticleCount = preview.Articles.Count,
                        LatestArticle = preview.Articles.Max(a => a.Published)
                    };
                    DialogResult = true;
                    return;
                }
            }

            StatusText.Text = F("Se caută canalul „{0}” pentru abonare...", video.ChannelName);
            var language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var channels = await YouTubeChannelSearchService.SearchAsync(video.ChannelName, language, cancellation.Token);
            var verifiedChannel = channels.FirstOrDefault(channel => channel.IsVerified);
            if (verifiedChannel is not null)
            {
                SelectedFeed = verifiedChannel;
                DialogResult = true;
            }
            else
            {
                StatusText.Text = F("Nu s-a putut găsi fluxul RSS pentru canalul „{0}”.", video.ChannelName);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception exception)
        {
            StatusText.Text = F("Abonarea la canal nu a reușit: {0}", exception.Message);
        }
        finally
        {
            _searchCancellation = null;
            cancellation.Dispose();
            SetControlsBusy(false);
            UpdateActionButtons();
        }
    }

    private void SetControlsBusy(bool busy)
    {
        SearchButton.IsEnabled = !busy;
        SearchType.IsEnabled = !busy;
        if (busy)
        {
            VerifyFeedButton.IsEnabled = false;
            OpenResultPageButton.IsEnabled = false;
            AddFeedButton.IsEnabled = false;
            OpenVideoButton.IsEnabled = false;
            OpenInAppButton.IsEnabled = false;
            SubscribeChannelButton.IsEnabled = false;
            ImportTranscriptButton.IsEnabled = false;
            LoadMoreButton.IsEnabled = false;
        }
    }

    private void Keywords_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        Search_Click(SearchButton, new RoutedEventArgs());
        e.Handled = true;
    }

    private void Results_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (SearchType.SelectedIndex == TypeVideosIndex)
            OpenVideo_Click(OpenVideoButton, new RoutedEventArgs());
        else if (Results.SelectedItem is DiscoveredFeed feed && !feed.IsVerified)
            VerifyFeed_Click(VerifyFeedButton, new RoutedEventArgs());
        else
            AddFeed_Click(AddFeedButton, new RoutedEventArgs());
        e.Handled = true;
    }

    private void Results_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Results.SelectedItem is null) return;
        if (SearchType.SelectedIndex == TypeVideosIndex)
            OpenVideo_Click(OpenVideoButton, new RoutedEventArgs());
        else if (Results.SelectedItem is DiscoveredFeed feed && !feed.IsVerified)
            VerifyFeed_Click(VerifyFeedButton, new RoutedEventArgs());
        else
            AddFeed_Click(AddFeedButton, new RoutedEventArgs());
    }

    private void ImportTranscript_Click(object sender, RoutedEventArgs e)
    {
        if (Results.SelectedItem is not YouTubeVideoSearchService.YouTubeVideoCandidate video) return;
        var dialog = new YouTubeTranscriptWindow(video) { Owner = this };
        dialog.ShowDialog();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        Close();
        e.Handled = true;
    }

    private static string T(string source) => UiText.Translate(source);
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);
}
