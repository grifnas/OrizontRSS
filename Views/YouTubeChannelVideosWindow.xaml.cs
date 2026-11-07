using System.Windows;
using System.Windows.Input;
using CititorRSS.Jaws.Localization;
using CititorRSS.Jaws.Services.Rss;

namespace CititorRSS.Jaws;

/// <summary>
/// Toate videoclipurile unui canal YouTube, citite din pagina canalului. Enter sau dublu clic deschid videoclipul
/// în fereastra Orizont, cu același comportament ca rezultatele căutării.
/// </summary>
public partial class YouTubeChannelVideosWindow : Window
{
    private readonly string _channelId;
    private readonly HashSet<string> _seenVideoIds = new(StringComparer.Ordinal);
    private string _continuation = string.Empty;
    private bool _loading;
    private bool _initialized;
    private readonly System.Collections.ObjectModel.ObservableCollection<YouTubeVideoSearchService.YouTubeVideoCandidate> _videos = new();

    public YouTubeChannelVideosWindow(string channelId, string channelName)
    {
        _channelId = channelId;
        InitializeComponent();
        Videos.ItemsSource = _videos;
        Title = UiText.Format("Videoclipurile canalului: {0}", channelName);
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;
        await LoadPageAsync(first: true);
    }

    private async Task LoadPageAsync(bool first)
    {
        if (_loading) return;
        _loading = true;
        UpdateButtons();
        StatusText.Text = T("Se încarcă videoclipurile canalului.");
        try
        {
            var page = first
                ? await YouTubeChannelVideosService.FirstPageAsync(_channelId)
                : await YouTubeChannelVideosService.NextPageAsync(_continuation);
            foreach (var video in page.Videos)
            {
                if (!_seenVideoIds.Add(video.VideoId)) continue;
                _videos.Add(video);
            }
            _continuation = page.ContinuationToken;

            if (first && _videos.Count > 0) Videos.SelectedIndex = 0;
            if (_videos.Count == 0)
                StatusText.Text = T("Canalul nu are videoclipuri de afișat.");
            else
                StatusText.Text = F("{0} videoclipuri încărcate.", _videos.Count) + " " +
                    (_continuation.Length > 0 ? T("Pentru mai multe videoclipuri apasă butonul de încărcare.") : T("Nu mai sunt videoclipuri de încărcat."));
        }
        catch (Exception exception)
        {
            StatusText.Text = F("Videoclipurile canalului nu au putut fi citite: {0}", exception.Message);
        }
        finally
        {
            _loading = false;
            UpdateButtons();
        }
    }

    private void UpdateButtons()
    {
        OpenButton.IsEnabled = !_loading && Videos.SelectedItem is YouTubeVideoSearchService.YouTubeVideoCandidate;
        LoadMoreButton.IsEnabled = !_loading && _continuation.Length > 0;
    }

    private void Videos_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => UpdateButtons();

    private void Videos_MouseDoubleClick(object sender, MouseButtonEventArgs e) => Open_Click(OpenButton, new RoutedEventArgs());

    private void Videos_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        Open_Click(OpenButton, new RoutedEventArgs());
        e.Handled = true;
    }

    private async void LoadMore_Click(object sender, RoutedEventArgs e) => await LoadPageAsync(first: false);

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        if (Videos.SelectedItem is not YouTubeVideoSearchService.YouTubeVideoCandidate video) return;
        try
        {
            var player = new YouTubePlayerWindow(video.VideoId, video.Title) { Owner = this };
            player.Closed += (_, _) => FocusVideoAfterPlayer(video);
            player.Show();
            StatusText.Text = T("Videoclipul se deschide în Orizont.");
        }
        catch (Exception exception)
        {
            StatusText.Text = F("Nu s-a putut deschide videoclipul în Orizont: {0}", exception.Message);
        }
    }

    /// <summary>După închiderea playerului, focusul revine pe videoclipul deschis din listă.</summary>
    private void FocusVideoAfterPlayer(YouTubeVideoSearchService.YouTubeVideoCandidate video)
    {
        if (!IsLoaded) return;
        if (!_videos.Contains(video)) { Videos.Focus(); return; }
        Videos.SelectedItem = video;
        Videos.ScrollIntoView(video);
        Videos.UpdateLayout();
        if (Videos.ItemContainerGenerator.ContainerFromItem(video) is System.Windows.Controls.ListBoxItem row) row.Focus();
        else Videos.Focus();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
    }

    private static string T(string source) => UiText.Translate(source);
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);
}
