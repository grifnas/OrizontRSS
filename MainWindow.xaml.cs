using System.Net.Http;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class MainWindow : Window
{
    private readonly FeedStore _store = new();
    private readonly RssReader _rss = new();
    private List<Feed> _feeds = [];
    private AppSettings _settings = new();
    private Feed? _feed;
    private Article? _article;
    private bool _folderAggregateView;
    private bool _suppressFolderFilter;
    private bool _suppressFeedSelection;
    private bool _suppressArticleSearch;
    private bool _suppressTagFilter;
    private bool _readNowView;
    private List<Article>? _readerReturnItems;
    private Article? _readerReturnArticle;
    private bool _isClosingAfterSave;
    private bool _closeInProgress;
    private bool _startupSucceeded;
    private readonly TaskCompletionSource<bool> _startupCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _isRefreshing;
    private CancellationTokenSource? _refreshCancellation;
    private TaskCompletionSource<bool>? _refreshCompletion;
    private List<Feed> _lastFailedFeeds = [];
    private int _refreshProcessed;
    private int _refreshTotal;
    private int _articleSelectionRevision;
    private bool _restoringSession;
    private readonly List<string> _statusHistory = [];
    private SpeechService? _speech;
    private const int MaximumStatusEntries = 500;
    private static string ProductVersion => AppVersionInfo.DisplayVersion;
    private const string AllFoldersKey = "{ORZONT:ALL_FOLDERS}";
    public MainWindow()
    {
        InitializeComponent();
        Title = AppVersionInfo.ProductTitle;
        var browserReaderLabel = T("Deschide articolul în modul Citire");
        var originalLabel = T("Deschide pagina originală în browser");
        var deepLLabel = T("Traduce cu DeepL");
        var whatsAppLabel = T("Distribuie prin WhatsApp");
        var browserReaderHelp = T("Deschide articolul fără reclame și elemente inutile în modul Citire Microsoft Edge, dacă pagina este compatibilă.");
        ArticleDeepLTranslateMenuItem.Header = ContentDeepLTranslateMenuItem.Header = deepLLabel;
        ArticleBrowserReaderModeMenuItem.Header = ContentBrowserReaderModeMenuItem.Header = browserReaderLabel;
        ArticleOriginalBrowserMenuItem.Header = ContentOriginalBrowserMenuItem.Header = originalLabel;
        ContentWhatsAppMenuItem.Header = whatsAppLabel;
        AutomationProperties.SetHelpText(ArticleBrowserReaderModeMenuItem, browserReaderHelp);
        AutomationProperties.SetHelpText(ContentBrowserReaderModeMenuItem, browserReaderHelp);
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _feeds = await _store.LoadAsync();
            _settings = await _store.LoadSettingsAsync();
            NormalizeSettings(_settings);
            _speech = new SpeechService();
            _speech.Configure(SpeechConfigurationFromSettings(_settings));
            _speech.StateChanged += Say;
            if (_settings.AutoCleanupEnabled) await CleanupExpiredArticlesAsync(false);
            RefreshFeedList();
            RefreshTagFilter();
            await RestoreSessionAsync();
            Say(_feeds.Count == 0
                ? F("Orizont RSS {0} este pregătit. Nu există feeduri. Alege butonul Adaugă feed.", ProductVersion)
                : F("Orizont RSS {0} este pregătit. S-au încărcat {1} feeduri. {2}", ProductVersion, _feeds.Count, ArticlePanelStatus()));
            _startupSucceeded = true;
            _startupCompletion.TrySetResult(true);
            if (_settings.UpdateAtStartup && _feeds.Count > 0)
                await RefreshFeedsAsync(_feeds.Where(feed => !feed.NeedsAttention).ToList());
        }
        catch (Exception exception)
        {
            var message = F("Orizont RSS nu a putut încărca în siguranță datele locale și se va închide fără să le suprascrie.\n\nDetalii: {0}", Describe(exception));
            Say("Pornirea a fost oprită deoarece datele locale nu au putut fi încărcate. Nu a fost suprascris niciun fișier.");
            MessageBox.Show(this, message, T("Pornire oprită pentru protejarea datelor"), MessageBoxButton.OK, MessageBoxImage.Error);
            _speech?.Dispose();
            _speech = null;
            _startupCompletion.TrySetResult(false);
            _isClosingAfterSave = true;
            Close();
        }
    }
    private async void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isClosingAfterSave) return;
        e.Cancel = true;
        if (_closeInProgress) return;
        _closeInProgress = true;
        _speech?.Stop(reportState: false);
        IsEnabled = false;
        try
        {
            if (!_startupCompletion.Task.IsCompleted)
            {
                Say("Se așteaptă încheierea încărcării inițiale înainte de închidere.");
                await _startupCompletion.Task;
            }
            if (!_startupSucceeded || _isClosingAfterSave) return;
            if (_isRefreshing)
            {
                Say("Se oprește actualizarea în curs înainte de închiderea aplicației.");
                _refreshCancellation?.Cancel();
                var completion = _refreshCompletion;
                if (completion is not null) await completion.Task;
            }
            Say("Se salvează feedurile înainte de închidere.");
            CaptureSessionState();
            await _store.SaveAsync(_feeds);
            await _store.SaveSettingsAsync(_settings);
            _isClosingAfterSave = true;
            _speech?.Dispose();
            _speech = null;
            Close();
        }
        catch (Exception exception)
        {
            IsEnabled = true;
            _closeInProgress = false;
            Say(F("Nu s-au putut salva feedurile: {0}", Describe(exception)));
            MessageBox.Show(this, F("Feedurile nu au putut fi salvate. Aplicația rămâne deschisă ca să nu pierzi modificările.\n\n{0}", Describe(exception)), T("Salvare eșuată"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private async Task RestoreSessionAsync()
    {
        _restoringSession = true;
        try
        {
            SelectFolder(_settings.LastFolder);
            AttentionFilter.IsChecked = _settings.LastAttentionFilter;
            FolderFilter.IsEnabled = AttentionFilter.IsChecked != true;
            UpdateAttentionButton();
            SelectComboItem(ArticleFilter, _settings.LastArticleFilter, "All");
            SelectComboItem(ArticleTimeFilter, _settings.LastTimeFilter, "Anytime");
            RefreshTagFilter();
            TagFilter.SelectedItem = TagFilter.Items.Cast<string>().FirstOrDefault(item => string.Equals(item, _settings.LastTagFilter, StringComparison.CurrentCultureIgnoreCase)) ?? "Toate etichetele";
            UpdateArticleFilterSummary();

            var savedFeed = _settings.LastFeedId is Guid feedId ? _feeds.FirstOrDefault(feed => feed.Id == feedId) : null;
            if (string.Equals(_settings.LastView, "Feed", StringComparison.OrdinalIgnoreCase) && savedFeed is not null)
            {
                _folderAggregateView = false;
                _readNowView = false;
                _feed = savedFeed;
                RefreshFeedList(savedFeed);
                RefreshArticleList(selectFirstWhenNoMatch: false);
            }
            else
            {
                _readNowView = string.Equals(_settings.LastView, "ReadNow", StringComparison.OrdinalIgnoreCase);
                ShowFolderAggregate(selectFirstWhenNoMatch: false);
            }

            var savedArticle = Articles.Items.Cast<Article>().FirstOrDefault(article =>
                (!string.IsNullOrWhiteSpace(_settings.LastArticleId) && string.Equals(article.Id, _settings.LastArticleId, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(_settings.LastArticleLink) && string.Equals(article.Link, _settings.LastArticleLink, StringComparison.OrdinalIgnoreCase)));
            if (savedArticle is not null) Articles.SelectedItem = savedArticle;
            else if (Articles.Items.Count > 0) Articles.SelectedIndex = 0;
            _article = Articles.SelectedItem as Article;
            Reader.Text = _article?.FullContent ?? _article?.Content ?? string.Empty;
        }
        finally
        {
            _restoringSession = false;
        }

        await Dispatcher.InvokeAsync(() =>
        {
            if (string.Equals(_settings.LastPanel, "Reader", StringComparison.OrdinalIgnoreCase) && _article is not null)
            {
                Reader.Focus();
                Reader.CaretIndex = 0;
                Reader.Select(0, 0);
            }
            else if (string.Equals(_settings.LastPanel, "Feeds", StringComparison.OrdinalIgnoreCase))
            {
                Feeds.Focus();
            }
            else if (_article is not null)
            {
                Articles.ScrollIntoView(_article);
                Articles.UpdateLayout();
                if (Articles.ItemContainerGenerator.ContainerFromItem(_article) is ListBoxItem row) Keyboard.Focus(row);
                else Articles.Focus();
            }
            else
            {
                Articles.Focus();
            }
        }, DispatcherPriority.ContextIdle);
    }

    private void CaptureSessionState()
    {
        var article = Articles.SelectedItem as Article ?? _article;
        _settings.LastFolder = SelectedFolder();
        _settings.LastFeedId = _feed?.Id;
        _settings.LastArticleId = article?.Id;
        _settings.LastArticleLink = article?.Link;
        _settings.LastArticleFilter = ComboText(ArticleFilter, "All");
        _settings.LastTimeFilter = ComboText(ArticleTimeFilter, "Anytime");
        _settings.LastTagFilter = TagFilter.SelectedItem as string ?? "Toate etichetele";
        _settings.LastView = _readNowView ? "ReadNow" : !_folderAggregateView && _feed is not null ? "Feed" : "Folder";
        _settings.LastPanel = ReaderPanel.IsKeyboardFocusWithin ? "Reader" : FeedPanel.IsKeyboardFocusWithin ? "Feeds" : "Articles";
        _settings.LastAttentionFilter = AttentionFilter.IsChecked == true;
    }

    private static void SelectComboItem(ComboBox combo, string wanted, string fallback)
    {
        var wantedKey = NormalizeComboKey(wanted);
        var fallbackKey = NormalizeComboKey(fallback);
        combo.SelectedItem = combo.Items.OfType<ComboBoxItem>().FirstOrDefault(item => string.Equals(ComboKey(item), wantedKey, StringComparison.OrdinalIgnoreCase) || string.Equals(item.Content?.ToString(), wanted, StringComparison.CurrentCultureIgnoreCase))
            ?? combo.Items.OfType<ComboBoxItem>().First(item => string.Equals(ComboKey(item), fallbackKey, StringComparison.OrdinalIgnoreCase));
    }

    private static string ComboKey(ComboBoxItem item) => item.Tag?.ToString() ?? item.Content?.ToString() ?? string.Empty;
    private static string ComboText(ComboBox combo, string fallback) => combo.SelectedItem is ComboBoxItem item ? ComboKey(item) : fallback;
    private static string ComboDisplayText(ComboBox combo, string fallback) => (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? UiText.Translate(fallback);
    private static string NormalizeComboKey(string value) => value switch
    {
        "Toate" => "All",
        "Necitite" => "Unread",
        "Favorite" => "Favorites",
        "De citit mai târziu" => "ReadLater",
        "Oricând" => "Anytime",
        "Astăzi" => "Today",
        "Ultimele 24 de ore" => "Last24Hours",
        "Ultimele 7 zile" => "Last7Days",
        "Ultimele 30 de zile" => "Last30Days",
        _ => value
    };
    private string SelectedFolder() => (FolderFilter.SelectedItem as FolderChoice)?.Key ?? AllFoldersKey;
    private void SelectFolder(string? wanted)
    {
        var key = string.IsNullOrWhiteSpace(wanted) || wanted == "Toate folderele" ? AllFoldersKey : wanted;
        FolderFilter.SelectedItem = FolderFilter.Items.OfType<FolderChoice>().FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.CurrentCultureIgnoreCase))
            ?? FolderFilter.Items.OfType<FolderChoice>().FirstOrDefault(item => item.Key == AllFoldersKey);
    }
    private void Feeds_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressFeedSelection || _restoringSession) return;
        _feed = Feeds.SelectedItem as Feed;
        if (_feed is null)
        {
            if (_folderAggregateView) return;
            Articles.ItemsSource = null;
            Reader.Clear();
            return;
        }
        _folderAggregateView = false;
        _readNowView = false;
        RefreshArticleList();
        _article = null;
        Reader.Clear();
        Say(F("Feed selectat: {0}. {1} articole.", _feed.Name, _feed.Articles.Count));
    }
    private void Feeds_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        var container = ItemsControl.ContainerFromElement(Feeds, e.OriginalSource as DependencyObject) as ListBoxItem;
        var clickedFeed = container?.DataContext as Feed;
        if (clickedFeed is not null && !Feeds.SelectedItems.Contains(clickedFeed))
        {
            Feeds.SelectedItems.Clear();
            Feeds.SelectedItems.Add(clickedFeed);
        }
        _feed = clickedFeed ?? Feeds.SelectedItem as Feed;
        if (_feed is null && Feeds.Items.Count > 0)
        {
            Feeds.SelectedIndex = 0;
            _feed = Feeds.SelectedItem as Feed;
        }
        if (_feed is not null) Feeds.SelectedItem = _feed;
        var selectedCount = SelectedFeeds().Count;
        RefreshFeedMenuItem.IsEnabled = selectedCount == 1;
        EditFeedMenuItem.IsEnabled = selectedCount == 1;
        DeleteFeedMenuItem.IsEnabled = selectedCount > 0;
        if (selectedCount == 0) Say("Nu există feeduri în această listă. Comanda Adaugă feed este disponibilă.");
    }
    private void Articles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _article = Articles.SelectedItem as Article;
        Reader.Text = _article?.FullContent ?? _article?.Content ?? string.Empty;
        if (_article is not null) Say(F("{0} Articol selectat: {1}.{2}", ArticlePanelStatus(), _article.Title, !string.IsNullOrWhiteSpace(_article.FullContent) ? UiText.Translate(" Text complet disponibil.") : string.Empty));
    }
    private async void Articles_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            FocusReader();
            e.Handled = true;
            return;
        }
        if (Keyboard.Modifiers != ModifierKeys.None) return;
        var selected = SelectedArticles();
        if (selected.Count == 0) return;
        if (e.Key == Key.R)
        {
            var markRead = selected.Any(article => !article.IsRead);
            await ChangeSelectedArticlesAsync(article => article.IsRead = markRead, markRead ? T("Articole marcate citite") : T("Articole marcate necitite"));
        }
        else if (e.Key == Key.F)
        {
            var addFavorite = selected.Any(article => !article.IsFavorite);
            await ChangeSelectedArticlesAsync(article => article.IsFavorite = addFavorite, addFavorite ? T("Articole adăugate la favorite") : T("Articole eliminate din favorite"));
        }
        else if (e.Key == Key.L)
        {
            var addReadLater = selected.Any(article => !article.ReadLater);
            await ChangeSelectedArticlesAsync(article => article.ReadLater = addReadLater, addReadLater ? T("Articole adăugate pentru mai târziu") : T("Articole eliminate din mai târziu"));
        }
        else return;
        e.Handled = true;
    }
    private void Articles_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        var container = ItemsControl.ContainerFromElement(Articles, e.OriginalSource as DependencyObject) as ListBoxItem;
        var clickedArticle = container?.DataContext as Article;
        if (clickedArticle is not null && !Articles.SelectedItems.Contains(clickedArticle))
        {
            Articles.SelectedItems.Clear();
            Articles.SelectedItems.Add(clickedArticle);
        }
        _article = clickedArticle ?? Articles.SelectedItem as Article;
        if (_article is null && Articles.Items.Count > 0)
        {
            Articles.SelectedIndex = 0;
            _article = Articles.SelectedItem as Article;
        }
        if (_article is not null)
        {
            var selected = SelectedArticles();
            var favoriteFilter = ArticleFilter.SelectedItem is ComboBoxItem favoriteItem && ComboKey(favoriteItem) == "Favorites";
            var readLaterFilter = ArticleFilter.SelectedItem is ComboBoxItem readLaterItem && ComboKey(readLaterItem) == "ReadLater";
            AddSelectedFavoritesMenuItem.Visibility = !favoriteFilter && selected.Any(article => !article.IsFavorite) ? Visibility.Visible : Visibility.Collapsed;
            RemoveSelectedFavoritesMenuItem.Visibility = selected.Any(article => article.IsFavorite) ? Visibility.Visible : Visibility.Collapsed;
            AddSelectedReadLaterMenuItem.Visibility = !readLaterFilter && selected.Any(article => !article.ReadLater) ? Visibility.Visible : Visibility.Collapsed;
            RemoveSelectedReadLaterMenuItem.Visibility = selected.Any(article => article.ReadLater) ? Visibility.Visible : Visibility.Collapsed;
            MarkSelectedReadMenuItem.Visibility = selected.Any(article => !article.IsRead) ? Visibility.Visible : Visibility.Collapsed;
            MarkSelectedUnreadMenuItem.Visibility = selected.Any(article => article.IsRead) ? Visibility.Visible : Visibility.Collapsed;
            return;
        }
        e.Handled = true;
        Say("Alege mai întâi un articol.");
    }
    private void ReadArticleMenu_Click(object sender, RoutedEventArgs e) => FocusReader();
    private async void LoadFullArticle_Click(object sender, RoutedEventArgs e)
    {
        if (_article is null || string.IsNullOrWhiteSpace(_article.Link)) { Say("Articolul nu are o adresă de unde poate fi adus textul complet."); return; }
        var article = _article;
        if (!string.IsNullOrWhiteSpace(article.FullContent))
        {
            Reader.Text = article.FullContent;
            Reader.Focus(); Reader.CaretIndex = 0; Reader.Select(0, 0);
            Say("Textul complet salvat este afișat. Folosește săgețile Sus și Jos pentru citire.");
            return;
        }
        Say("Se aduce textul complet de pe pagina articolului.");
        try
        {
            var text = await _rss.LoadReadableContentAsync(article.Link);
            if (text.Length < 150) throw new InvalidOperationException("Pagina nu a oferit suficient text pentru modul de citire.");
            article.FullContent = text;
            Reader.Text = text;
            await _store.SaveAsync(_feeds);
            Reader.Focus(); Reader.CaretIndex = 0; Reader.Select(0, 0);
            Say(F("Textul complet a fost adus și salvat local. {0} caractere. Folosește săgețile Sus și Jos pentru citire.", text.Length));
        }
        catch (Exception exception)
        {
            Say(F("Nu s-a putut aduce textul complet: {0} Deschide articolul în browser pentru pagina originală.", Describe(exception)));
        }
    }
    private async void DeepLTranslate_Click(object sender, RoutedEventArgs e)
    {
        if (_article is null) { Say(T("Alege mai întâi un articol pentru traducere.")); return; }
        if (!_settings.DeepLEnabled) { ShowAiProblem(T("DeepL nu este activat. Deschide Setări, Setări Inteligență artificială, testează cheia DeepL și apasă Salvează."), MessageBoxImage.Warning); return; }
        string key;
        try { key = SecretProtector.Unprotect(_settings.EncryptedDeepLKey); }
        catch { ShowAiProblem(T("Cheia DeepL salvată nu poate fi citită pentru acest cont Windows. Introdu cheia din nou în Setări Inteligență artificială."), MessageBoxImage.Warning); return; }
        if (string.IsNullOrWhiteSpace(key)) { ShowAiProblem(T("Nu există o cheie DeepL salvată. Deschide Setări Inteligență artificială."), MessageBoxImage.Warning); return; }
        var source = !string.IsNullOrWhiteSpace(Reader.Text) ? Reader.Text : !string.IsNullOrWhiteSpace(_article.FullContent) ? _article.FullContent : _article.Content;
        if (string.IsNullOrWhiteSpace(source)) { Say(T("Articolul nu conține text pentru traducere.")); return; }
        Say(F("Se traduce articolul cu DeepL în limba interfeței. Așteaptă."));
        try
        {
            var translated = await new DeepLConnection().TranslateAsync(key, source, DeepLConnection.TargetLanguageForUi());
            Reader.Text = translated;
            Reader.Focus(); Reader.CaretIndex = 0; Reader.Select(0, 0);
            Say(T("Articolul a fost tradus cu DeepL."));
        }
        catch (Exception exception)
        {
            Say(F("DeepL nu a putut traduce articolul: {0}", Describe(exception)));
            MessageBox.Show(this, F("DeepL nu a putut traduce articolul: {0}", Describe(exception)), T("Traducere cu DeepL"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    private async void OpenArticleInBrowser_Click(object sender, RoutedEventArgs e)
    {
        if (_article is null || string.IsNullOrWhiteSpace(_article.Link)) { Say("Articolul nu are o adresă care poate fi deschisă."); return; }
        try
        {
            Say("Se pregătește articolul în cititorul Orizont.");
            string text;
            try
            {
                // Reextragem pagina pentru a nu reutiliza un FullContent vechi, care poate
                // conține reclame sau cod JavaScript înaintea curățării extractorului.
                text = await _rss.LoadReadableContentAsync(_article.Link);
            }
            catch when (!string.IsNullOrWhiteSpace(_article.FullContent))
            {
                text = _article.FullContent;
                Say("Pagina nu a putut fi reîmprospătată; se folosește ultima versiune salvată a articolului.");
            }
            if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("Pagina nu a oferit conținut lizibil.");
            _article.FullContent = text;
            await _store.SaveAsync(_feeds);
            var window = new ArticleReaderWindow(
                _article,
                text,
                _settings,
                async () =>
                {
                    var refreshed = await _rss.LoadReadableContentAsync(_article.Link);
                    _article.FullContent = refreshed;
                    await _store.SaveAsync(_feeds);
                    return refreshed;
                },
                () => _store.SaveSettingsAsync(_settings),
                _speech!,
                () => _store.SaveAsync(_feeds)) { Owner = this };
            window.Show();
            Say("Articolul este deschis în cititorul Orizont.");
        }
        catch (Exception exception)
        {
            Say(F("Articolul nu a putut fi deschis în cititorul Orizont: {0}", Describe(exception)));
            MessageBox.Show(this, F("Articolul nu a putut fi deschis în cititorul Orizont.\n\n{0}", Describe(exception)), T("Deschidere nereușită"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void OpenOriginalArticleInBrowser_Click(object sender, RoutedEventArgs e)
    {
        if (_article is null || string.IsNullOrWhiteSpace(_article.Link)) { Say("Articolul nu are o adresă care poate fi deschisă."); return; }
        try
        {
            BrowserReaderService.OpenOriginal(_article.Link);
            Say("Pagina originală a articolului a fost deschisă în browserul implicit.");
        }
        catch (Exception exception)
        {
            Say(F("Pagina originală nu a putut fi deschisă în browser: {0}", Describe(exception)));
            MessageBox.Show(this, F("Pagina originală nu a putut fi deschisă în browser.\n\n{0}", Describe(exception)), T("Deschidere nereușită"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void OpenBrowserReadingMode_Click(object sender, RoutedEventArgs e)
    {
        if (_article is null || string.IsNullOrWhiteSpace(_article.Link)) { Say("Articolul nu are o adresă care poate fi deschisă."); return; }
        try
        {
            var mode = BrowserReaderService.OpenReadingMode(_article.Link);
            Say(mode == BrowserOpenMode.ReadingMode
                ? T("Articolul a fost trimis în modul Citire Microsoft Edge. Dacă pagina este compatibilă, reclamele și elementele inutile sunt eliminate.")
                : T("Microsoft Edge nu a fost găsit. Articolul a fost deschis normal în browserul implicit."));
        }
        catch (Exception exception)
        {
            Say(F("Articolul nu a putut fi deschis în modul Citire: {0}", Describe(exception)));
            MessageBox.Show(this, F("Articolul nu a putut fi deschis în modul Citire.\n\n{0}", Describe(exception)), T("Deschidere nereușită"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void Reader_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (_article is not null) return;
        e.Handled = true;
        Say("Alege mai întâi un articol pentru comenzile de copiere și distribuire.");
    }
    private async void Reader_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        if (_settings.StopSpeechWhenLeavingArticle) _speech?.Stop(reportState: false);
        var returnArticle = _readerReturnArticle ?? _article;
        var priorItems = _readerReturnItems ?? Articles.Items.Cast<Article>().ToList();
        var anchorIndex = returnArticle is null ? -1 : priorItems.IndexOf(returnArticle);
        var selectionRevision = ++_articleSelectionRevision;
        _readerReturnItems = null;
        _readerReturnArticle = null;
        e.Handled = true;

        RefreshArticleList(selectFirstWhenNoMatch: false);
        var primary = returnArticle is not null && Articles.Items.Contains(returnArticle)
            ? returnArticle
            : priorItems.Skip(Math.Max(0, anchorIndex + 1)).FirstOrDefault(article => Articles.Items.Contains(article))
              ?? priorItems.Take(Math.Max(0, anchorIndex)).Reverse().FirstOrDefault(article => Articles.Items.Contains(article));

        await RestoreArticleSelectionAsync([], primary, selectionRevision);
        await _store.SaveAsync(_feeds);
        Say(primary is null ? ArticlePanelStatus() : F("{0} Înapoi la articolul: {1}.", ArticlePanelStatus(), primary.Title));
    }
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.F1 && Keyboard.Modifiers == ModifierKeys.None)
        {
            Help_Click(this, e);
            e.Handled = true;
            return;
        }
        if (key == Key.F11 && Keyboard.Modifiers == ModifierKeys.None)
        {
            ToggleWindowSize_Click(this, e);
            e.Handled = true;
            return;
        }
        if (key == Key.F9 && Keyboard.Modifiers == ModifierKeys.Shift)
        {
            VoiceSettings_Click(this, e);
            e.Handled = true;
            return;
        }
        if (key == Key.F9 && Keyboard.Modifiers == ModifierKeys.None)
        {
            if (_speech?.IsSpeaking == true || _speech?.IsPaused == true)
                PauseResumeSpeech_Click(this, e);
            else
                SpeakContent_Click(this, e);
            e.Handled = true;
            return;
        }
        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt))
        {
            if (key == Key.V) SpeakContent_Click(this, e);
            else if (key == Key.P) PauseResumeSpeech_Click(this, e);
            else if (key == Key.S) StopSpeech_Click(this, e);
            else goto ContinueKeyHandling;
            e.Handled = true;
            return;
        }
        if (key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None && _isRefreshing)
        {
            StopRefresh_Click(this, e);
            e.Handled = true;
            return;
        }
        if (key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None && (_speech?.IsSpeaking == true || _speech?.IsPaused == true))
        {
            _speech.Stop();
            if (!Reader.IsKeyboardFocusWithin) e.Handled = true;
            return;
        }
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (key is Key.D1 or Key.NumPad1) FocusFeeds();
            else if (key is Key.D2 or Key.NumPad2) FocusArticles();
            else if (key is Key.D3 or Key.NumPad3) FocusReader();
            else if (key is Key.D4 or Key.NumPad4) ReadNow_Click(this, e);
            else goto ContinueKeyHandling;
            e.Handled = true;
            return;
        }
ContinueKeyHandling:
        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            if (key is Key.D1 or Key.NumPad1) FocusFolderSelector();
            else if (key is Key.D2 or Key.NumPad2) FocusSelectedFolderArticles();
            else goto ContinueCommandHandling;
            e.Handled = true;
            return;
        }
ContinueCommandHandling:
        if (key == Key.H && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            StatusHistory_Click(this, e);
            e.Handled = true;
            return;
        }
        if (key == Key.F && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            ArticleFilters_Click(this, e);
            e.Handled = true;
            return;
        }
        if (key == Key.U && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            ShowUnreadOverview_Click(this, e);
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.None)
        {
            if (FolderFilter.IsKeyboardFocusWithin) DeleteFolder_Click(this, e);
            else if (Feeds.IsKeyboardFocusWithin) Delete_Click(this, e);
            else if (Articles.IsKeyboardFocusWithin) DeleteSelectedArticles_Click(this, e);
            else return;
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Space && MainMenu.IsKeyboardFocusWithin)
        {
            var menuItem = FindFocusedMenuItem(Keyboard.FocusedElement as DependencyObject);
            if (menuItem is not null && menuItem.HasItems)
            {
                menuItem.IsSubmenuOpen = true;
                menuItem.Focus();
                e.Handled = true;
                return;
            }
        }
        if (e.Key == Key.R && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            LoadFullArticle_Click(this, e);
            e.Handled = true;
            return;
        }
        if ((e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control) || e.Key == Key.F3)
        {
            ArticleSearch.Focus();
            ArticleSearch.SelectAll();
            Say("Caută în articole.");
            e.Handled = true;
            return;
        }
        if (e.Key != Key.F6 || (Keyboard.Modifiers != ModifierKeys.None && Keyboard.Modifiers != ModifierKeys.Shift)) return;
        if (Keyboard.Modifiers == ModifierKeys.Shift)
        {
            if (FeedPanel.IsKeyboardFocusWithin) FocusReader();
            else if (ArticlePanel.IsKeyboardFocusWithin) FocusFeeds();
            else if (ReaderPanel.IsKeyboardFocusWithin) FocusArticles();
            else FocusReader();
        }
        else if (FeedPanel.IsKeyboardFocusWithin) FocusArticles();
        else if (ArticlePanel.IsKeyboardFocusWithin) FocusReader();
        else if (ReaderPanel.IsKeyboardFocusWithin) FocusFeeds();
        else FocusFeeds();
        e.Handled = true;
    }
    private void FocusFeeds()
    {
        Feeds.Focus();
        if (Feeds.SelectedItem is null && Feeds.Items.Count > 0) Feeds.SelectedIndex = 0;
        var selected = Feeds.SelectedItem as Feed;
        Say(selected is null
            ? F("Panou Feeduri. {0} feeduri afișate.", Feeds.Items.Count)
            : F("Panou Feeduri. {0} feeduri afișate. Feed selectat: {1}.", Feeds.Items.Count, selected.Name));
    }
    private void FocusFolderSelector()
    {
        if (AttentionFilter.IsChecked == true)
        {
            AttentionToggleButton.Focus();
            Say("Selectorul de foldere nu este disponibil cât timp sunt afișate feedurile care necesită atenție. Apasă butonul pentru a reveni la foldere.");
            return;
        }
        FolderFilter.Focus();
        var folder = SelectedFolder();
        Say(folder == AllFoldersKey
            ? F("Selector foldere. Este selectată opțiunea Toate folderele. {0} foldere disponibile. Folosește săgețile pentru alegere, apoi Ctrl+Shift+2 pentru articole.", Math.Max(0, FolderFilter.Items.Count - 1))
            : F("Selector foldere. Folder selectat: {0}. Folosește săgețile pentru alegere, apoi Ctrl+Shift+2 pentru articole.", folder));
    }
    private void FocusSelectedFolderArticles()
    {
        _readNowView = false;
        ShowFolderAggregate();
        FocusArticles();
    }
    private void FocusArticles()
    {
        Articles.Focus();
        if (Articles.SelectedItem is null && Articles.Items.Count > 0) Articles.SelectedIndex = 0;
        var selected = Articles.SelectedItem as Article;
        Say(selected is null ? ArticlePanelStatus() : F("{0} Articol selectat: {1}.", ArticlePanelStatus(), selected.Title));
    }
    private void FocusReader()
    {
        if (_article is null)
        {
            Reader.Focus();
            Say("Conținut articol, gol. Alege un articol din listă pentru a-l citi.");
            return;
        }
        var article = _article;
        _readerReturnItems = Articles.Items.Cast<Article>().ToList();
        _readerReturnArticle = article;
        foreach (var copy in RelatedArticleCopies([article])) copy.IsRead = true;
        Reader.Focus();
        Reader.CaretIndex = 0;
        Reader.Select(0, 0);
        Say(F("Conținut articol: {0}. Editare numai în citire. Folosește săgețile Sus și Jos pentru citire.", article.Title));
    }
    private string ArticlePanelStatus()
    {
        var displayed = Articles?.Items.Cast<Article>().ToList() ?? [];
        var unread = displayed.Count(article => !article.IsRead);
        var context = string.Empty;
        if (_folderAggregateView)
        {
            var folder = SelectedFolder();
            var feedCount = DisplayedFeeds(folder).Count();
            context = folder == AllFoldersKey
                ? F("Toate folderele: {0} feeduri. ", feedCount)
                : F("Folder {0}: {1} feeduri. ", folder, feedCount);
        }
        return context + F("Panou Articole. {0} articole afișate: {1} necitite, {2} citite. {3}", displayed.Count, unread, displayed.Count - unread, ArticleFiltersStatusSentence());
    }
    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        if (RejectDataChangeDuringRefresh(T("adăugarea unui feed"))) return;
        var dialog = new FeedDialog { Owner = this };
        dialog.SetFolders(AvailableFolders());
        if (dialog.ShowDialog() != true) return;
        var duplicate = FindFeedByAddress(dialog.FeedUrl);
        if (duplicate is not null)
        {
            Say(F("Feedul nu a fost adăugat deoarece adresa este deja abonată: {0}.", duplicate.Name));
            MessageBox.Show(this, F("Această adresă este deja folosită de feedul „{0}”, în folderul „{1}”.\n\nOrizont RSS nu permite adăugarea aceluiași feed de două ori.", duplicate.Name, duplicate.Folder), T("Feed deja existent"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var feed = new Feed { Name = dialog.FeedName, Url = dialog.FeedUrl, Folder = dialog.FolderName };
        _feeds.Add(feed);
        try
        {
            await _store.SaveAsync(_feeds);
        }
        catch (Exception exception)
        {
            _feeds.Remove(feed);
            Say(F("Feedul nu a putut fi salvat: {0}", Describe(exception)));
            MessageBox.Show(this, F("Feedul nu a fost adăugat deoarece datele nu au putut fi salvate.\n\n{0}", Describe(exception)), T("Salvare feed eșuată"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        RefreshFeedList(feed);
        Say(F("Feed adăugat și salvat: {0}. Apasă Actualizează feed.", feed.Name));
    }
    private async void Settings_Click(object sender, RoutedEventArgs e)
    {
        await OpenSettingsAsync(SettingsWindow.SettingsSection.Application);
    }
    private async void VoiceSettings_Click(object sender, RoutedEventArgs e)
    {
        await OpenSettingsAsync(SettingsWindow.SettingsSection.Voice);
    }
    private async Task OpenSettingsAsync(SettingsWindow.SettingsSection section)
    {
        var dialog = new SettingsWindow(_settings, section) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        await _store.SaveSettingsAsync(_settings);
        _speech?.Configure(SpeechConfigurationFromSettings(_settings));
        RefreshArticleList(selectFirstWhenNoMatch: false);
        if (section != SettingsWindow.SettingsSection.Application)
        {
            Say(section == SettingsWindow.SettingsSection.Voice
                ? T("Setări voce")
                : T("Setările Inteligență artificială au fost salvate."));
            return;
        }
        if (dialog.LanguageChanged)
        {
            const string languageMessage = "Limba interfeței a fost salvată. Noua limbă va fi folosită după repornirea Orizont RSS.";
            Say(UiText.Translate(languageMessage));
            MessageBox.Show(this, UiText.Translate(languageMessage), UiText.Translate("Limba interfeței"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
        if (dialog.CleanupRequested)
        {
            if (RejectDataChangeDuringRefresh(T("curățarea articolelor expirate"))) return;
            var count = CountExpiredArticles();
            if (count == 0) { Say("Nu există articole obișnuite expirate de curățat. Favoritele și articolele pentru mai târziu sunt protejate."); return; }
            var answer = MessageBox.Show(this, F("Vor fi șterse {0} articole obișnuite mai vechi de {1} zile. Favoritele și articolele pentru mai târziu nu sunt afectate. Continui?", count, _settings.RetentionDays), T("Confirmă curățarea"), MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (answer == MessageBoxResult.Yes) await CleanupExpiredArticlesAsync(true);
            else Say("Curățarea a fost anulată.");
            return;
        }
        Say(_settings.AutoCleanupEnabled ? F("Setări salvate. Articolele obișnuite sunt păstrate {0} zile.", _settings.RetentionDays) : T("Setări salvate. Curățarea automată este dezactivată."));
    }
    private async void AiSettings_Click(object sender, RoutedEventArgs e)
    {
        await OpenSettingsAsync(SettingsWindow.SettingsSection.Ai);
    }
    private int CountExpiredArticles()
    {
        var cutoff = DateTimeOffset.Now.AddDays(-_settings.RetentionDays);
        return ArticleRetention.CountExpired(_feeds, cutoff);
    }
    private void FocusArticleSearch_Click(object sender, RoutedEventArgs e)
    {
        ArticleSearch.Focus();
        ArticleSearch.SelectAll();
        Say("Caută în articole.");
    }
    private async void ArticleFilters_Click(object sender, RoutedEventArgs e)
    {
        var currentState = ComboText(ArticleFilter, "All");
        var currentPeriod = ComboText(ArticleTimeFilter, "Anytime");
        var currentTag = TagFilter.SelectedItem as string ?? "Toate etichetele";
        var availableTags = _feeds.SelectMany(feed => feed.Articles)
            .SelectMany(article => article.Tags ?? [])
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(tag => tag)
            .ToList();
        var dialog = new ArticleFiltersDialog(currentState, currentPeriod, currentTag, availableTags) { Owner = this };
        if (dialog.ShowDialog() != true)
        {
            ArticleFiltersButton.Focus();
            Say(F("Filtrele articolelor nu au fost modificate. {0}", ArticleFiltersStatusSentence()));
            return;
        }

        var position = CaptureArticlePosition();
        _readNowView = false;
        SelectComboItem(ArticleFilter, dialog.SelectedState, "All");
        SelectComboItem(ArticleTimeFilter, dialog.SelectedPeriod, "Anytime");
        TagFilter.SelectedItem = TagFilter.Items.Cast<string>().FirstOrDefault(item => string.Equals(item, dialog.SelectedTag, StringComparison.CurrentCultureIgnoreCase)) ?? "Toate etichetele";
        UpdateArticleFilterSummary();
        RefreshArticleList(selectFirstWhenNoMatch: position.Items.Count == 0);
        if (position.Items.Count > 0) await RestoreCapturedPositionAsync(position);
        else if (Articles.Items.Count > 0)
        {
            if (Articles.SelectedItem is null) Articles.SelectedIndex = 0;
            Articles.Focus();
        }
        else ArticleFiltersButton.Focus();
        Say(F("Filtre aplicate. {0}", ArticlePanelStatus()));
    }

    private void UpdateArticleFilterSummary()
    {
        if (ArticleFilterSummary is null || ArticleFiltersButton is null) return;
        var description = ArticleFiltersDescription();
        ArticleFilterSummary.Text = F("Filtre active: {0}.", description);
        AutomationProperties.SetName(ArticleFilterSummary, F("Filtre active: {0}", description));
        AutomationProperties.SetName(ArticleFiltersButton, F("Filtre articole. Active: {0}", description));
    }

    private string ArticleFiltersDescription()
    {
        var active = new List<string>();
        var state = ComboText(ArticleFilter, "All");
        var period = ComboText(ArticleTimeFilter, "Anytime");
        var tag = TagFilter.SelectedItem as string ?? "Toate etichetele";
        if (state != "All") active.Add(ComboDisplayText(ArticleFilter, "Toate"));
        if (period != "Anytime") active.Add(ComboDisplayText(ArticleTimeFilter, "Oricând").ToLower(CultureInfo.CurrentCulture));
        if (tag != "Toate etichetele") active.Add(F("eticheta {0}", tag));
        return active.Count == 0 ? UiText.Translate("niciunul") : string.Join(", ", active);
    }

    private string ArticleFiltersStatusSentence() => F("Filtre active: {0}.", ArticleFiltersDescription());
    private void ReadNow_Click(object sender, RoutedEventArgs e)
    {
        AttentionFilter.IsChecked = false;
        _suppressFolderFilter = true;
        SelectFolder(AllFoldersKey);
        _suppressFolderFilter = false;
        SelectComboItem(ArticleFilter, "All", "All");
        SelectComboItem(ArticleTimeFilter, "Anytime", "Anytime");
        RefreshTagFilter();
        _suppressTagFilter = true;
        TagFilter.SelectedItem = "Toate etichetele";
        _suppressTagFilter = false;
        UpdateArticleFilterSummary();
        _readNowView = true;
        ShowFolderAggregate();
        Articles.Focus();
        Say(F("Citește acum: {0} articole noi din ultimele {1} zile, favorite și articole pentru mai târziu.", Articles.Items.Count, Math.Max(1, _settings.ReadNowFavoriteDays)));
    }
    private void AiNoteLibrary_Click(object sender, RoutedEventArgs e)
    {
        var count = _feeds.Sum(feed => feed.Articles.Sum(article => article.AiNotes?.Count ?? 0));
        if (count == 0) { Say("Nu există încă notițe AI salvate."); return; }
        new AiNoteLibraryWindow(_feeds) { Owner = this }.ShowDialog();
    }
    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this,
            F("{0}\n\nCitește, organizează și înțelege informațiile din sursele tale preferate.\n\nOrizont RSS a fost creat de Grigore Frișan în colaborare cu OpenAI Codex.\n\nF1 deschide ajutorul. Ctrl+Shift+H deschide istoricul stării și al erorilor. Licența completă poate fi deschisă din meniul Ajutor.", VersionInformation()),
            T("Despre Orizont RSS"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
    private void Help_Click(object sender, RoutedEventArgs e)
    {
        new HelpWindow { Owner = this }.ShowDialog();
    }
    private void OpenUserGuide_Click(object sender, RoutedEventArgs e)
    {
        var guidePath = UserGuideLocator.Find();
        if (guidePath is null)
        {
            Say("Ghidul utilizatorului nu a fost găsit.");
            MessageBox.Show(this, T("Ghidul utilizatorului nu a putut fi găsit. Copiază din nou toate fișierele distribuției Orizont RSS."), T("Ghid indisponibil"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo(guidePath) { UseShellExecute = true });
            Say("Ghidul complet al utilizatorului a fost deschis în browserul implicit.");
        }
        catch (Exception exception)
        {
            Say(F("Ghidul utilizatorului nu a putut fi deschis: {0}", Describe(exception)));
            MessageBox.Show(this, F("Ghidul utilizatorului nu a putut fi deschis.\n\n{0}", Describe(exception)), T("Ghid indisponibil"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void OpenLicense_Click(object sender, RoutedEventArgs e)
    {
        var licensePath = Path.Combine(AppContext.BaseDirectory, "LICENSE");
        if (!File.Exists(licensePath))
        {
            Say("Fișierul licenței open-source nu a fost găsit.");
            MessageBox.Show(this, T("Fișierul LICENSE nu a putut fi găsit. Copiază din nou toate fișierele distribuției Orizont RSS."), T("Licență indisponibilă"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo(licensePath) { UseShellExecute = true });
            Say("Licența GNU General Public License a fost deschisă în aplicația implicită pentru fișiere text.");
        }
        catch (Exception exception)
        {
            Say(F("Licența nu a putut fi deschisă: {0}", Describe(exception)));
            MessageBox.Show(this, F("Licența nu a putut fi deschisă.\n\n{0}", Describe(exception)), T("Licență indisponibilă"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void StatusHistory_Click(object sender, RoutedEventArgs e)
    {
        new StatusHistoryWindow(_statusHistory, () => _statusHistory.Clear()) { Owner = this }.ShowDialog();
    }
    private void CopyVersionInfo_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(VersionInformation());
        Say("Informațiile despre versiune au fost copiate în clipboard.");
    }
    private static string VersionInformation()
    {
        var executable = Environment.ProcessPath;
        var built = !string.IsNullOrWhiteSpace(executable) && File.Exists(executable) ? File.GetLastWriteTime(executable) : DateTime.Now;
        return F("Orizont RSS {0}\nEdiție compilată la {1}\nWindows pe 64 de biți\nCopyright © 2026 Grigore Frișan\nCreat în colaborare cu OpenAI Codex\nLicență: GNU GPL versiunea 3 sau ulterioară", ProductVersion, built.ToString("g", CultureInfo.CurrentCulture));
    }

    private void SpeakContent_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureSpeechAvailable()) return;
        var text = Reader.SelectionLength > 0 ? Reader.SelectedText : ArticleSpeechDocument();
        if (string.IsNullOrWhiteSpace(text)) { Say("Nu există conținut de citit cu voce."); return; }
        _speech!.Speak(text);
    }

    private void SpeakFromCursor_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureSpeechAvailable()) return;
        if (string.IsNullOrWhiteSpace(Reader.Text)) { Say("Nu există conținut de citit cu voce."); return; }
        var start = Math.Clamp(Reader.CaretIndex, 0, Reader.Text.Length);
        var text = Reader.Text[start..];
        if (string.IsNullOrWhiteSpace(text)) { Say("Cursorul se află la sfârșitul conținutului."); return; }
        _speech!.Speak(text);
    }

    private void PauseResumeSpeech_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureSpeechAvailable()) return;
        if (!_speech!.PauseOrResume()) Say("Nu există o citire vocală în curs pentru pauză sau continuare.");
    }

    private void StopSpeech_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureSpeechAvailable()) return;
        if (!_speech!.IsSpeaking && !_speech.IsPaused) { Say("Nu există o citire vocală în curs."); return; }
        _speech.Stop();
    }

    private void ToggleWindowSize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        Say(WindowState == WindowState.Maximized
            ? T("Fereastra a fost maximizată.")
            : T("Fereastra a fost restabilită."));
    }

    private bool EnsureSpeechAvailable()
    {
        if (_speech?.EnsureAvailable() == true) return true;
        var message = T("Motorul vocal selectat nu este disponibil. Verifică motorul și vocea în Setări.");
        Say(message);
        MessageBox.Show(this, T(message), T("Citire vocală indisponibilă"), MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private string ArticleSpeechDocument()
    {
        if (_article is null) return Reader.Text;
        var source = _feeds.FirstOrDefault(feed => feed.Articles.Contains(_article))?.Name;
        var sourceLine = string.IsNullOrWhiteSpace(source) ? string.Empty : F("Sursă: {0}.{1}", source, Environment.NewLine);
        return F("Titlu: {0}.{1}{2}Data publicării: {3}.{1}{1}{4}", _article.Title, Environment.NewLine, sourceLine, _article.Published.ToString("f", CultureInfo.CurrentCulture), Reader.Text);
    }
    private async void AiSummarize_Click(object sender, RoutedEventArgs e) => await AskGeminiAsync(T("Rezumat"), T("Rezuma articolul în limba interfeței. Evidențiază faptele, contextul și incertitudinile."));
    private async void AiTranslate_Click(object sender, RoutedEventArgs e) => await AskGeminiAsync(T("Traducere"), T("Tradu articolul complet în limba interfeței. Nu adăuga comentarii, păstrează structura paragrafului."));
    private async void AiExplain_Click(object sender, RoutedEventArgs e) => await AskGeminiAsync(T("Explicație"), T("Explică articolul în limba interfeței, simplu și clar, pentru un cititor nespecialist."));
    private async void AiKeyPoints_Click(object sender, RoutedEventArgs e) => await AskGeminiAsync(T("Ideile principale"), T("Extrage ideile principale ale articolului într-o listă clară, în limba interfeței."));
    private async void AiDiscuss_Click(object sender, RoutedEventArgs e)
    {
        if (_article is null) { Say("Alege mai întâi un articol."); return; }
        var dialog = new AiQuestionWindow { Owner = this };
        if (dialog.ShowDialog() != true) return;
        await AskGeminiAsync(T("Răspuns Gemini"), dialog.UserQuestion);
    }
    private void ShowAiNotes_Click(object sender, RoutedEventArgs e)
    {
        if (_article is null) { Say("Alege mai întâi un articol."); return; }
        _article.AiNotes ??= [];
        if (_article.AiNotes.Count == 0) { Say("Acest articol nu are încă notițe AI salvate."); return; }
        new AiNotesWindow(_article) { Owner = this }.ShowDialog();
    }
    private async void EditSelectedTags_Click(object sender, RoutedEventArgs e)
    {
        var selected = SelectedArticles();
        if (selected.Count == 0) { Say("Alege unul sau mai multe articole pentru etichete."); return; }
        var position = CaptureArticlePosition();
        var common = selected.Select(article => article.Tags ?? []).Aggregate((left, right) => left.Intersect(right, StringComparer.CurrentCultureIgnoreCase).ToList());
        var available = _feeds.SelectMany(feed => feed.Articles).SelectMany(article => article.Tags ?? []);
        var dialog = new TagDialog(common, available) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        if (dialog.TagsToDelete.Count > 0)
        {
            foreach (var article in _feeds.SelectMany(feed => feed.Articles))
                article.Tags = (article.Tags ?? []).Where(tag => !dialog.TagsToDelete.Contains(tag, StringComparer.CurrentCultureIgnoreCase)).ToList();
            RefreshTagFilter();
            RefreshArticleList(selectFirstWhenNoMatch: false);
            await _store.SaveAsync(_feeds);
            await RestoreCapturedPositionAsync(position);
            Say(dialog.TagsToDelete.Count == 1
                ? F("Eticheta „{0}” a fost ștearsă din toate articolele.", dialog.TagsToDelete[0])
                : F("Etichete șterse din toate articolele: {0}.", dialog.TagsToDelete.Count));
            return;
        }
        foreach (var article in RelatedArticleCopies(selected)) article.Tags = dialog.Tags.ToList();
        RefreshTagFilter();
        RefreshArticleList(selectFirstWhenNoMatch: false);
        await _store.SaveAsync(_feeds);
        await RestoreCapturedPositionAsync(position);
        Say(F("Etichete salvate pentru {0} articole.", selected.Count));
    }
    private async Task AskGeminiAsync(string title, string request)
    {
        if (_article is null) { ShowAiProblem(T("Alege mai întâi un articol."), MessageBoxImage.Information); return; }
        if (!_settings.GeminiEnabled) { ShowAiProblem(T("Gemini nu este activat. Deschide Setări, Setări Inteligență artificială, testează cheia Gemini și apasă Salvează."), MessageBoxImage.Warning); return; }
        string key;
        try { key = SecretProtector.Unprotect(_settings.EncryptedGeminiKey); }
        catch { ShowAiProblem(T("Cheia Gemini salvată nu poate fi citită pentru acest cont Windows. Introdu cheia din nou în Setări Inteligență artificială."), MessageBoxImage.Warning); return; }
        if (string.IsNullOrWhiteSpace(key)) { ShowAiProblem(T("Nu există o cheie Gemini salvată. Deschide Setări Inteligență artificială."), MessageBoxImage.Warning); return; }
        var source = !string.IsNullOrWhiteSpace(Reader.Text) ? Reader.Text : !string.IsNullOrWhiteSpace(_article.FullContent) ? _article.FullContent : _article.Content;
        if (string.IsNullOrWhiteSpace(source)) { ShowAiProblem(T("Articolul nu conține text pentru Gemini. Alege Adu textul complet de pe site și încearcă din nou."), MessageBoxImage.Warning); return; }
        var article = _article;
        Say(F("AI: Gemini pregătește {0} pentru: {1}. Așteaptă.", title.ToLower(CultureInfo.CurrentCulture), article.Title));
        try
        {
            var prompt = $"Limba interfeței: {CultureInfo.CurrentUICulture.NativeName}\nTitlu articol: {article.Title}\nSursă: {article.Link}\n\nCerere: {request}\n\nArticol:\n{source}";
            var response = await new GeminiConnection().GenerateAsync(key, _settings.AiInstructions, prompt);
            var initialPrompt = prompt;
            new AiResponseWindow(
                title,
                response,
                article.Title,
                article.Link,
                followUp => new GeminiConnection().GenerateAsync(key, _settings.AiInstructions, $"{initialPrompt}\n\n{followUp}"),
                async noteContent =>
                {
                    article.AiNotes ??= [];
                    article.AiNotes.Add(new AiNote { Title = title, Content = noteContent });
                    await _store.SaveAsync(_feeds);
                },
                _speech) { Owner = this }.ShowDialog();
            Say("Răspunsul Gemini a fost închis. Ești înapoi la lista de articole.");
        }
        catch (TaskCanceledException) { ShowAiProblem(T("Gemini nu a răspuns în 45 de secunde. Încearcă din nou mai târziu."), MessageBoxImage.Error); }
        catch (HttpRequestException) { ShowAiProblem(T("Nu s-a putut ajunge la Gemini. Verifică internetul, firewall-ul sau proxy-ul."), MessageBoxImage.Error); }
        catch (Exception exception) { ShowAiProblem(F("Gemini nu a putut executa comanda „{0}”.\n\nDetalii: {1}", title, exception.Message), MessageBoxImage.Error); }
    }
    private void ShowAiProblem(string message, MessageBoxImage icon)
    {
        Say(F("AI: {0}", message.Replace(Environment.NewLine, " ")));
        MessageBox.Show(this, T(message), T("AI — stare comandă"), MessageBoxButton.OK, icon);
    }
    private async Task CleanupExpiredArticlesAsync(bool announce)
    {
        var position = Articles?.Items.Count > 0 ? CaptureArticlePosition() : null;
        var cutoff = DateTimeOffset.Now.AddDays(-_settings.RetentionDays);
        var removed = ArticleRetention.RemoveExpired(_feeds, cutoff);
        if (removed > 0)
        {
            await _store.SaveAsync(_feeds);
            if (position is not null)
            {
                RefreshArticleList(selectFirstWhenNoMatch: false);
                await RestoreCapturedPositionAsync(position);
            }
        }
        if (announce) Say(F("Curățare încheiată. {0} articole obișnuite expirate au fost șterse. Favoritele și articolele pentru mai târziu nu au fost afectate.", removed));
    }
    private async void DiscoverSite_Click(object sender, RoutedEventArgs e)
    {
        if (RejectDataChangeDuringRefresh(T("adăugarea unui feed descoperit"))) return;
        var dialog = new DiscoverSiteWindow { Owner = this };
        if (dialog.ShowDialog() != true || dialog.SelectedFeed is null) return;
        var candidate = dialog.SelectedFeed;
        if (FindFeedByAddress(candidate.Url) is Feed duplicate) { Say(F("Acest feed este deja abonat cu numele {0}.", duplicate.Name)); return; }
        var folderDialog = new FeedDialog { Owner = this };
        folderDialog.SetFolders(AvailableFolders());
        folderDialog.SetValues(candidate.Name, candidate.Url, "Neorganizate");
        if (folderDialog.ShowDialog() != true) return;
        var feed = new Feed { Name = folderDialog.FeedName, Url = folderDialog.FeedUrl, Folder = folderDialog.FolderName };
        _feeds.Add(feed);
        await _store.SaveAsync(_feeds);
        RefreshFeedList(feed);
        Say(F("Feed verificat adăugat: {0}. {1} articole sunt disponibile la sursă.", feed.Name, candidate.ArticleCount));
    }
    private async void DiscoverTopic_Click(object sender, RoutedEventArgs e)
    {
        if (RejectDataChangeDuringRefresh(T("adăugarea unui feed din catalog"))) return;
        var dialog = new DiscoverTopicWindow { Owner = this };
        if (dialog.ShowDialog() != true || dialog.SelectedFeed is null) return;
        var candidate = dialog.SelectedFeed;
        if (FindFeedByAddress(candidate.Url) is Feed duplicate) { Say(F("Acest feed este deja abonat cu numele {0}.", duplicate.Name)); return; }
        var feedDialog = new FeedDialog { Owner = this };
        feedDialog.SetFolders(AvailableFolders());
        feedDialog.SetValues(candidate.Name, candidate.Url, candidate.SourceSite);
        if (feedDialog.ShowDialog() != true) return;
        var feed = new Feed { Name = feedDialog.FeedName, Url = feedDialog.FeedUrl, Folder = feedDialog.FolderName };
        _feeds.Add(feed);
        await _store.SaveAsync(_feeds);
        RefreshFeedList(feed);
        Say(F("Feed verificat adăugat: {0}. {1} articole sunt disponibile la sursă.", feed.Name, candidate.ArticleCount));
    }
    private async void ImportOpml_Click(object sender, RoutedEventArgs e)
    {
        if (RejectDataChangeDuringRefresh("importul OPML")) return;
        var dialog = new OpenFileDialog { Title = T("Importă abonamente OPML"), Filter = T("Fișiere OPML (*.opml;*.xml)|*.opml;*.xml|Toate fișierele (*.*)|*.*"), CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var document = XDocument.Load(dialog.FileName);
            var candidates = new List<Feed>();
            foreach (var outline in document.Descendants().Where(element => element.Name.LocalName == "outline"))
            {
                var address = outline.Attribute("xmlUrl")?.Value?.Trim();
                if (string.IsNullOrWhiteSpace(address) || !Uri.TryCreate(address, UriKind.Absolute, out _)) continue;
                var name = outline.Attribute("text")?.Value?.Trim() ?? outline.Attribute("title")?.Value?.Trim() ?? address;
                var folder = outline.Ancestors().FirstOrDefault(element => element.Name.LocalName == "outline" && element.Attribute("xmlUrl") is null)?.Attribute("text")?.Value?.Trim();
                candidates.Add(new Feed { Name = name, Url = address, Folder = string.IsNullOrWhiteSpace(folder) ? "Neorganizate" : folder });
            }
            var existing = new HashSet<string>(_feeds.Select(feed => DuplicateCleaner.FeedKey(feed.Url)), StringComparer.OrdinalIgnoreCase);
            var added = 0;
            foreach (var feed in candidates)
            {
                if (!existing.Add(DuplicateCleaner.FeedKey(feed.Url))) continue;
                _feeds.Add(feed);
                added++;
            }
            await _store.SaveAsync(_feeds);
            RefreshFeedList();
            Say(F("Import OPML încheiat. {0} feeduri adăugate. Începe actualizarea feedurilor importate.", added));
            await RefreshFeedsAsync(candidates.Where(feed => _feeds.Contains(feed)).ToList());
        }
        catch (Exception exception)
        {
            Say(F("Importul OPML a eșuat: {0}", exception.Message));
        }
    }
    private void ExportOpml_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Title = T("Exportă abonamente OPML"), Filter = T("Fișier OPML (*.opml)|*.opml"), FileName = "Orizont-abonamente.opml", AddExtension = true, DefaultExt = ".opml", OverwritePrompt = true };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var body = new XElement("body", _feeds.GroupBy(feed => string.IsNullOrWhiteSpace(feed.Folder) ? "Neorganizate" : feed.Folder).OrderBy(group => group.Key).Select(group => new XElement("outline", new XAttribute("text", group.Key), new XAttribute("title", group.Key), group.OrderBy(feed => feed.Name).Select(feed => new XElement("outline", new XAttribute("text", feed.Name), new XAttribute("title", feed.Name), new XAttribute("type", "rss"), new XAttribute("xmlUrl", feed.Url))))));
            var document = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement("opml", new XAttribute("version", "2.0"), new XElement("head", new XElement("title", "Abonamente Orizont RSS"), new XElement("dateCreated", DateTimeOffset.Now.ToString("R"))), body));
            document.Save(dialog.FileName);
            Say(F("Export OPML încheiat. {0} feeduri au fost salvate în fișier.", _feeds.Count));
        }
        catch (Exception exception)
        {
            Say(F("Exportul OPML a eșuat: {0}", exception.Message));
        }
    }
    private async void Backup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Title = T("Creează copie de siguranță"), Filter = T("Copie de siguranță Orizont (*.orizont-backup.json)|*.orizont-backup.json"), FileName = $"Orizont-backup-{DateTime.Now:yyyy-MM-dd}.orizont-backup.json", AddExtension = true, DefaultExt = ".orizont-backup.json", OverwritePrompt = true };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var backup = new BackupDocument { Feeds = _feeds, Settings = BackupSettings(_settings) };
            await using var stream = File.Create(dialog.FileName);
            await JsonSerializer.SerializeAsync(stream, backup, new JsonSerializerOptions { WriteIndented = true });
            Say(F("Copie de siguranță creată. {0} feeduri au fost salvate. Cheia Gemini nu este inclusă.", _feeds.Count));
        }
        catch (Exception exception)
        {
            Say(F("Nu s-a putut crea copia de siguranță: {0}", exception.Message));
        }
    }
    private async void RestoreBackup_Click(object sender, RoutedEventArgs e)
    {
        if (RejectDataChangeDuringRefresh(T("restaurarea copiei de siguranță"))) return;
        var dialog = new OpenFileDialog { Title = T("Restaurează copie de siguranță"), Filter = T("Copie de siguranță Orizont (*.orizont-backup.json)|*.orizont-backup.json|Fișiere JSON (*.json)|*.json"), CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            await using var stream = File.OpenRead(dialog.FileName);
            var backup = await JsonSerializer.DeserializeAsync<BackupDocument>(stream);
            if (backup is null || backup.FormatVersion != 1 || backup.Feeds is null || backup.Settings is null) throw new InvalidOperationException(T("Fișierul nu este o copie de siguranță Orizont RSS compatibilă."));
            if (MessageBox.Show(this, F("Restabilești {0} feeduri și articolele lor? Lista locală actuală va fi înlocuită. Cheia Gemini de pe acest calculator nu este afectată.", backup.Feeds.Count), T("Confirmă restaurarea"), MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return;
            backup.Feeds = backup.Feeds.Where(feed => feed is not null).ToList();
            foreach (var feed in backup.Feeds)
            {
                feed.Articles ??= [];
                feed.Articles = feed.Articles.Where(article => article is not null).ToList();
                foreach (var article in feed.Articles)
                {
                    article.Tags ??= [];
                    article.AiNotes ??= [];
                }
            }
            var currentGeminiKey = _settings.EncryptedGeminiKey;
            var currentGeminiEnabled = _settings.GeminiEnabled;
            _feeds = backup.Feeds;
            _settings = backup.Settings;
            NormalizeSettings(_settings);
            _settings.EncryptedGeminiKey = currentGeminiKey;
            _settings.GeminiEnabled = currentGeminiEnabled && !string.IsNullOrWhiteSpace(currentGeminiKey);
            _speech?.Configure(SpeechConfigurationFromSettings(_settings));
            _feed = null;
            _article = null;
            AttentionFilter.IsChecked = false;
            await _store.SaveAsync(_feeds);
            await _store.SaveSettingsAsync(_settings);
            RefreshFeedList();
            ShowFolderAggregate();
            Reader.Clear();
            Say(F("Restaurare încheiată. {0} feeduri au fost încărcate. Cheia Gemini locală a fost păstrată.", _feeds.Count));
        }
        catch (Exception exception)
        {
            Say(F("Restaurarea a eșuat: {0}", exception.Message));
        }
    }
    private static AppSettings BackupSettings(AppSettings settings) => BackupPolicy.SanitizeSettings(settings);
    private static void NormalizeSettings(AppSettings settings)
    {
        settings.UiLanguage = UiCulture.NormalizeSelection(settings.UiLanguage);
        if (settings.RetentionDays is not (1 or 3 or 7 or 14 or 30 or 60 or 90 or 180 or 365)) settings.RetentionDays = 90;
        settings.SpeechRate = Math.Clamp(settings.SpeechRate, -10, 10);
        settings.SpeechVolume = Math.Clamp(settings.SpeechVolume, 0, 100);
        settings.SpeechEngine = SpeechEngineIds.Normalize(settings.SpeechEngine);
        settings.EspeakVoiceName = string.IsNullOrWhiteSpace(settings.EspeakVoiceName) ? "ro" : settings.EspeakVoiceName;
        settings.GeminiVoiceName = string.IsNullOrWhiteSpace(settings.GeminiVoiceName) ? "Charon" : settings.GeminiVoiceName;
        settings.EspeakPitch = Math.Clamp(settings.EspeakPitch, 0, 100);
        settings.AiInstructions = string.IsNullOrWhiteSpace(settings.AiInstructions) ? "Răspunde în limba interfeței. Fii clar, concis și semnalează incertitudinile." : settings.AiInstructions;
        settings.LastFolder = string.IsNullOrWhiteSpace(settings.LastFolder) || settings.LastFolder == "Toate folderele" ? AllFoldersKey : settings.LastFolder;
        settings.LastArticleFilter = string.IsNullOrWhiteSpace(settings.LastArticleFilter) ? "Toate" : settings.LastArticleFilter;
        settings.LastTimeFilter = string.IsNullOrWhiteSpace(settings.LastTimeFilter) ? "Oricând" : settings.LastTimeFilter;
        settings.LastTagFilter = string.IsNullOrWhiteSpace(settings.LastTagFilter) ? "Toate etichetele" : settings.LastTagFilter;
        settings.LastView = string.IsNullOrWhiteSpace(settings.LastView) ? "Folder" : settings.LastView;
        settings.LastPanel = string.IsNullOrWhiteSpace(settings.LastPanel) ? "Articles" : settings.LastPanel;
    }

    private static SpeechConfiguration SpeechConfigurationFromSettings(AppSettings settings) => new(
        settings.SpeechEngine,
        settings.SpeechVoiceName,
        settings.EspeakVoiceName,
        settings.SpeechRate,
        settings.SpeechVolume,
        settings.EspeakPitch,
        settings.GeminiVoiceName,
        UnprotectGeminiKey(settings));

    private static string? UnprotectGeminiKey(AppSettings settings)
    {
        try { return SecretProtector.Unprotect(settings.EncryptedGeminiKey); }
        catch { return null; }
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (RejectDataChangeDuringRefresh(T("editarea unui feed"))) return;
        if (_feed is null) { Say("Alege un feed pentru editare."); return; }
        var dialog = new FeedDialog(_feed) { Owner = this };
        dialog.SetFolders(AvailableFolders());
        if (dialog.ShowDialog() != true) return;
        var duplicate = FindFeedByAddress(dialog.FeedUrl, _feed);
        if (duplicate is not null)
        {
            Say(F("Modificarea a fost respinsă. Adresa este deja folosită de feedul {0}.", duplicate.Name));
            MessageBox.Show(this, F("Adresa introdusă este deja folosită de feedul „{0}”, din folderul „{1}”.\n\nFeedul curent nu a fost modificat.", duplicate.Name, duplicate.Folder), T("Adresă deja abonată"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var feed = _feed;
        var previousName = feed.Name;
        var previousUrl = feed.Url;
        var previousFolder = feed.Folder;
        feed.Name = dialog.FeedName;
        feed.Url = dialog.FeedUrl;
        feed.Folder = dialog.FolderName;
        try
        {
            await _store.SaveAsync(_feeds);
        }
        catch (Exception exception)
        {
            feed.Name = previousName;
            feed.Url = previousUrl;
            feed.Folder = previousFolder;
            Say(F("Modificarea feedului nu a putut fi salvată: {0}", Describe(exception)));
            MessageBox.Show(this, F("Feedul a rămas nemodificat deoarece datele nu au putut fi salvate.\n\n{0}", Describe(exception)), T("Salvare feed eșuată"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        RefreshFeedList(feed);
        Say(F("Feed editat și salvat: {0}.", feed.Name));
    }
    private Feed? FindFeedByAddress(string address, Feed? except = null)
    {
        var key = DuplicateCleaner.FeedKey(address);
        return _feeds.FirstOrDefault(feed => !ReferenceEquals(feed, except) && string.Equals(DuplicateCleaner.FeedKey(feed.Url), key, StringComparison.OrdinalIgnoreCase));
    }

    private async void CleanDuplicates_Click(object sender, RoutedEventArgs e)
    {
        if (RejectDataChangeDuringRefresh(T("curățarea duplicatelor"))) return;
        var analysis = DuplicateCleaner.Analyze(_feeds);
        if (!analysis.HasDuplicates)
        {
            Say("Verificare încheiată. Nu au fost găsite feeduri sau articole duplicate.");
            MessageBox.Show(this, T("Nu au fost găsite feeduri duplicate și nici articole duplicate în interiorul feedurilor.\n\nArticolele asemănătoare publicate de surse diferite nu sunt considerate duplicate."), T("Verificare duplicate"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var report = F("Au fost găsite:\n\n• {0} grupuri cu aceeași adresă de feed;\n• {1} copii suplimentare de feeduri cu aceeași adresă;\n• {2} copii suplimentare de articole;\n• {3} perechi de feeduri cu cel puțin 90% articole comune.\n\nDuplicatele certe vor fi comasate. Pentru fiecare pereche cu adrese diferite și conținut foarte asemănător, Orizont RSS te va întreba separat ce feed păstrezi. Poți păstra ambele.\n\nFavoritele, articolele pentru mai târziu, etichetele, textele complete și notițele AI vor fi păstrate.\n\nContinui cu verificarea și curățarea?", analysis.FeedGroups, analysis.ExtraFeedCopies, analysis.DuplicateArticleCopies, analysis.OverlappingFeeds.Count);
        Say(F("Verificare duplicate: {0} grupuri cu aceeași adresă, {1} copii de articole și {2} perechi de feeduri suprapuse. Se așteaptă confirmarea.", analysis.FeedGroups, analysis.DuplicateArticleCopies, analysis.OverlappingFeeds.Count));
        if (MessageBox.Show(this, report, T("Confirmă curățarea duplicatelor"), MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
        {
            Say("Curățarea duplicatelor a fost anulată. Nu s-a modificat nimic.");
            return;
        }

        var position = Articles.Items.Count > 0 ? CaptureArticlePosition() : null;
        var selectedFeedKey = _feed is null ? null : DuplicateCleaner.FeedKey(_feed.Url);
        var wasAggregate = _folderAggregateView;
        var result = DuplicateCleaner.Clean(_feeds);
        _feed = selectedFeedKey is null ? null : _feeds.FirstOrDefault(feed => string.Equals(DuplicateCleaner.FeedKey(feed.Url), selectedFeedKey, StringComparison.OrdinalIgnoreCase));
        var removedOverlappingFeeds = 0;
        var removedOverlappingArticles = 0;
        foreach (var overlap in analysis.OverlappingFeeds)
        {
            if (!_feeds.Contains(overlap.First) || !_feeds.Contains(overlap.Second)) continue;
            var choice = MessageBox.Show(this,
                F("Aceste feeduri au adrese diferite, dar {0} din {1} articole ale feedului mai mic sunt comune, adică {2}%.\n\nPRIMUL: {3}\nFolder: {4}\nAdresă: {5}\n\nAL DOILEA: {6}\nFolder: {7}\nAdresă: {8}\n\nAlege:\nDA = păstrează primul și elimină al doilea;\nNU = păstrează al doilea și elimină primul;\nANULEAZĂ = păstrează ambele.", overlap.CommonArticles, overlap.SmallerFeedArticles, overlap.OverlapPercent, overlap.First.Name, overlap.First.Folder, overlap.First.Url, overlap.Second.Name, overlap.Second.Folder, overlap.Second.Url),
                T("Posibile feeduri suprapuse"), MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
            if (choice == MessageBoxResult.Cancel) continue;
            var keep = choice == MessageBoxResult.Yes ? overlap.First : overlap.Second;
            var remove = choice == MessageBoxResult.Yes ? overlap.Second : overlap.First;
            if (ReferenceEquals(_feed, remove)) _feed = keep;
            removedOverlappingArticles += DuplicateCleaner.MergeFeedPair(_feeds, keep, remove);
            removedOverlappingFeeds++;
        }
        await _store.SaveAsync(_feeds);
        RefreshTagFilter();

        if (!wasAggregate && _feed is not null)
        {
            RefreshFeedList();
            if (!Feeds.Items.Contains(_feed))
            {
                _suppressFolderFilter = true;
                SelectFolder(_feed.Folder);
                _suppressFolderFilter = false;
                RefreshFeedList();
            }
            _suppressFeedSelection = true;
            Feeds.SelectedItem = _feed;
            _suppressFeedSelection = false;
            _folderAggregateView = false;
            _readNowView = false;
            RefreshArticleList(selectFirstWhenNoMatch: false);
        }
        else
        {
            ShowFolderAggregate(selectFirstWhenNoMatch: false);
        }
        if (position is not null) await RestoreCapturedPositionAsync(position);
        var totalRemovedFeeds = result.RemovedFeedCopies + removedOverlappingFeeds;
        var totalRemovedArticles = result.RemovedArticleCopies + removedOverlappingArticles;
        Say(F("Curățare duplicate încheiată. {0} feeduri și {1} copii de articole au fost eliminate prin comasare.", totalRemovedFeeds, totalRemovedArticles));
        MessageBox.Show(this, F("Curățarea s-a încheiat.\n\nFeeduri eliminate prin comasare: {0}.\nArticole duplicate eliminate: {1}.\nPerechi de feeduri suprapuse păstrate sau ignorate: {2}.\n\nMarcajele și notițele articolelor au fost păstrate.", totalRemovedFeeds, totalRemovedArticles, analysis.OverlappingFeeds.Count - removedOverlappingFeeds), T("Duplicate curățate"), MessageBoxButton.OK, MessageBoxImage.Information);
    }
    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (RejectDataChangeDuringRefresh(T("ștergerea feedurilor"))) return;
        var selected = SelectedFeeds();
        if (selected.Count == 0) { Say("Alege unul sau mai multe feeduri pentru ștergere."); return; }
        var description = selected.Count == 1 ? F("feedul „{0}”", selected[0].Name) : F("cele {0} feeduri selectate", selected.Count);
        if (MessageBox.Show(this, F("Ștergi {0} și articolele lor salvate?", description), T("Confirmă ștergerea"), MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        foreach (var feed in selected) _feeds.Remove(feed);
        _feed = null; _article = null;
        RefreshFeedList();
        Articles.ItemsSource = null; Reader.Clear();
        await _store.SaveAsync(_feeds);
        Say(selected.Count == 1 ? F("Feed șters: {0}.", selected[0].Name) : F("Feeduri șterse: {0}.", selected.Count));
        Feeds.Focus();
    }
    private async void DeleteSelectedArticles_Click(object sender, RoutedEventArgs e)
    {
        if (RejectDataChangeDuringRefresh(T("ștergerea articolelor"))) return;
        var selected = SelectedArticles();
        if (selected.Count == 0) { Say("Alege unul sau mai multe articole pentru ștergere."); return; }
        var targets = RelatedArticleCopies(selected).ToHashSet();
        var additionalCopies = targets.Count - selected.Count;
        var noun = selected.Count == 1 ? T("articolul selectat") : F("cele {0} articole selectate", selected.Count);
        var copiesWarning = additionalCopies > 0 ? F(" Vor fi eliminate și {0} copii identice din alte feeduri.", additionalCopies) : string.Empty;
        if (MessageBox.Show(this, F("Ștergi {0}?{1} Această acțiune elimină și eventualele marcaje Favorite sau De citit mai târziu.", noun, copiesWarning), T("Confirmă ștergerea"), MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        var position = CaptureArticlePosition();
        foreach (var feed in _feeds) feed.Articles.RemoveAll(article => targets.Contains(article));
        _article = null;
        Reader.Clear();
        RefreshArticleList(selectFirstWhenNoMatch: false);
        await _store.SaveAsync(_feeds);
        await RestoreCapturedPositionAsync(position, keepAnchorWhenVisible: false);
        Say(targets.Count == selected.Count
            ? F("Articole șterse: {0}.", selected.Count)
            : F("Articole selectate șterse: {0}. Au fost eliminate și {1} copii identice din alte feeduri.", selected.Count, targets.Count - selected.Count));
    }
    private async void DeleteFolder_Click(object sender, RoutedEventArgs e)
    {
        if (RejectDataChangeDuringRefresh(T("ștergerea unui folder"))) return;
        var folder = SelectedFolder();
        if (folder == AllFoldersKey) { Say("Alege un folder concret pentru ștergere."); return; }
        var affected = _feeds.Where(feed => string.Equals(feed.Folder, folder, StringComparison.CurrentCultureIgnoreCase)).ToList();
        if (affected.Count == 0) { Say("Folderul nu conține feeduri și nu mai este disponibil."); return; }
        var message = F("Ștergi folderul „{0}”? Cele {1} feeduri și articolele lor vor fi păstrate și mutate în folderul Neorganizate.", folder, affected.Count);
        if (MessageBox.Show(this, message, T("Confirmă ștergerea folderului"), MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        foreach (var feed in affected) feed.Folder = "Neorganizate";
        await _store.SaveAsync(_feeds);
        RefreshFeedList();
        ShowFolderAggregate();
        Say(F("Folder șters: {0}. {1} feeduri au fost mutate în Neorganizate.", folder, affected.Count));
        FolderFilter.Focus();
    }
    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (_feed is null) { Say("Alege un feed pentru actualizare."); return; }
        await RefreshFeedsAsync([_feed]);
    }
    private async void RefreshAll_Click(object sender, RoutedEventArgs e)
    {
        var eligible = _feeds.Where(feed => !feed.NeedsAttention).ToList();
        var skipped = _feeds.Count - eligible.Count;
        if (skipped > 0) Say(F("{0} feeduri necesită atenție și nu vor fi actualizate automat. Selectează un feed și alege Actualizează feedul pentru reîncercare.", skipped));
        await RefreshFeedsAsync(eligible);
    }
    private void StopRefresh_Click(object sender, RoutedEventArgs e)
    {
        if (!_isRefreshing || _refreshCancellation is null)
        {
            Say("Nu există nicio actualizare în curs.");
            return;
        }
        if (_refreshCancellation.IsCancellationRequested)
        {
            Say("Oprirea a fost deja cerută. Se întrerupe conexiunea curentă.");
            return;
        }
        var answer = MessageBox.Show(this,
            F("Au fost procesate {0} din {1} feeduri. Oprești actualizarea acum? Conexiunea curentă va fi întreruptă, iar datele deja primite vor fi păstrate.", _refreshProcessed, _refreshTotal),
            T("Oprește actualizarea"), MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes) return;
        _refreshCancellation.Cancel();
        StopRefreshMenuItem.IsEnabled = false;
        Say(F("S-a cerut oprirea actualizării. Procesate {0} din {1}; se întrerupe conexiunea curentă.", _refreshProcessed, _refreshTotal));
    }

    private async void RetryFailed_Click(object sender, RoutedEventArgs e)
    {
        var failed = _lastFailedFeeds.Where(_feeds.Contains).Distinct().ToList();
        if (failed.Count == 0)
        {
            RetryFailedMenuItem.IsEnabled = false;
            Say("Nu există feeduri cu eroare de reîncercat din ultima actualizare.");
            return;
        }
        await RefreshFeedsAsync(failed);
    }

    private void FeedDetails_Click(object sender, RoutedEventArgs e)
    {
        var feed = Feeds.SelectedItem as Feed ?? _feed;
        if (feed is null) { Say("Alege mai întâi un feed."); return; }
        var lastSuccess = feed.LastSuccessfulUpdate?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? T("niciodată");
        var lastArticle = feed.LastArticleReceivedOn?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? T("necunoscut");
        var state = feed.NeedsAttention ? F("Necesită atenție: {0}.", feed.AttentionReason) : T("Nu necesită atenție.");
        var error = string.IsNullOrWhiteSpace(feed.LastError) ? T("Nicio eroare înregistrată.") : F("Ultima eroare: {0}", feed.LastError);
        var details = F("{0}\n\nStare: {1}\nFolder: {2}\nAdresă: {3}\nArticole salvate: {4}\nErori consecutive: {5}\nUltima actualizare reușită: {6}\nUltimul articol primit: {7}\n{8}", feed.Name, state, feed.Folder, feed.Url, feed.Articles.Count, feed.ConsecutiveFailures, lastSuccess, lastArticle, error);
        Say(F("Starea feedului {0}: {1} {2} articole, {3} erori consecutive.", feed.Name, state, feed.Articles.Count, feed.ConsecutiveFailures));
        MessageBox.Show(this, details, T("Detalii despre starea feedului"), MessageBoxButton.OK,
            feed.NeedsAttention ? MessageBoxImage.Warning : MessageBoxImage.Information);
    }

    private async Task RefreshFeedsAsync(List<Feed> feedsToUpdate)
    {
        if (feedsToUpdate.Count == 0) { Say("Nu există feeduri de actualizat."); return; }
        if (_isRefreshing) { Say("O actualizare a feedurilor este deja în curs."); return; }
        _isRefreshing = true;
        _refreshCancellation = new CancellationTokenSource();
        _refreshCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _refreshProcessed = 0;
        _refreshTotal = feedsToUpdate.Count;
        StopRefreshMenuItem.IsEnabled = true;
        RetryFailedMenuItem.IsEnabled = false;
        var articlePosition = Articles.Items.Count > 0 ? CaptureArticlePosition() : null;
        var timer = Stopwatch.StartNew();
        try
        {
            var selected = _feed;
            var successes = 0;
            var failures = 0;
            var articlesAdded = 0;
            var articlesIgnoredByRetention = 0;
            var failureNames = new List<string>();
            var failedFeeds = new List<Feed>();
            var cancelled = false;
            Say(F("Se actualizează {0} feeduri. Procesul poate dura câteva minute. Escape poate opri procesul și întrerupe conexiunea curentă.", feedsToUpdate.Count));
            foreach (var feed in feedsToUpdate)
            {
                if (_refreshCancellation.IsCancellationRequested) { cancelled = true; break; }
                var failed = false;
                var feedCancelled = false;
                try
                {
                    var downloaded = DuplicateCleaner.DeduplicateArticles(await _rss.LoadAsync(feed.Url, _refreshCancellation.Token), out _);
                    var old = feed.Articles
                        .Where(article => !string.IsNullOrWhiteSpace(article.Id))
                        .GroupBy(article => article.Id, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
                    foreach (var article in downloaded.Where(article => old.TryGetValue(article.Id, out _)))
                    {
                        article.IsRead = old[article.Id].IsRead;
                        article.IsFavorite = old[article.Id].IsFavorite;
                        article.ReadLater = old[article.Id].ReadLater;
                        article.Tags = old[article.Id].Tags ?? [];
                        article.AiNotes = old[article.Id].AiNotes ?? [];
                        article.FullContent = old[article.Id].FullContent;
                    }
                    var accepted = downloaded;
                    if (_settings.AutoCleanupEnabled)
                    {
                        var cutoff = DateTimeOffset.Now.AddDays(-_settings.RetentionDays);
                        accepted = downloaded.Where(article => article.Published >= cutoff || (old.TryGetValue(article.Id, out var previous) && (previous.IsFavorite || previous.ReadLater))).ToList();
                        articlesIgnoredByRetention += downloaded.Count - accepted.Count;
                        var protectedSaved = feed.Articles.Where(article => article.Published < cutoff && (article.IsFavorite || article.ReadLater))
                            .Where(article => string.IsNullOrWhiteSpace(article.Id) || !accepted.Any(current => string.Equals(current.Id, article.Id, StringComparison.OrdinalIgnoreCase)));
                        accepted.AddRange(protectedSaved);
                    }
                    var newlyReceived = accepted.Count(article => !old.ContainsKey(article.Id));
                    articlesAdded += newlyReceived;
                    feed.Articles = accepted;
                    feed.ConsecutiveFailures = 0;
                    feed.LastError = null;
                    feed.LastSuccessfulUpdate = DateTimeOffset.Now;
                    if (newlyReceived > 0) feed.LastArticleReceivedOn = DateTimeOffset.Now;
                    successes++;
                }
                catch (OperationCanceledException) when (_refreshCancellation.IsCancellationRequested)
                {
                    feedCancelled = true;
                    cancelled = true;
                }
                catch (Exception exception)
                {
                    failed = true;
                    failures++;
                    failureNames.Add(feed.Name);
                    failedFeeds.Add(feed);
                    feed.ConsecutiveFailures++;
                    feed.LastError = Describe(exception);
                }
                finally
                {
                    if (!feedCancelled)
                    {
                        _refreshProcessed++;
                        var progress = F("Actualizare: {0} din {1} feeduri procesate; {2} reușite, {3} cu eroare, {4} articole noi.", _refreshProcessed, _refreshTotal, successes, failures, articlesAdded);
                        SetProgressStatus(progress, failed || _refreshProcessed % 10 == 0 || _refreshProcessed == _refreshTotal);
                    }
                }
                if (feedCancelled) break;
            }
            if (_refreshCancellation.IsCancellationRequested && _refreshProcessed < _refreshTotal) cancelled = true;
            _lastFailedFeeds = failedFeeds;
            RetryFailedMenuItem.IsEnabled = failedFeeds.Count > 0;
            await _store.SaveAsync(_feeds);
            RefreshFeedList(selected);
            RefreshTagFilter();
            if (selected is not null)
            {
                _feed = selected;
                RefreshArticleList(selectFirstWhenNoMatch: false);
            }
            else if (_folderAggregateView)
            {
                ShowFolderAggregate(selectFirstWhenNoMatch: false);
            }
            if (articlePosition is not null) await RestoreCapturedPositionAsync(articlePosition);
            var needsAttention = _feeds.Count(feed => feed.NeedsAttention);
            var elapsed = timer.Elapsed;
            var duration = elapsed.TotalMinutes >= 1
                ? F("{0} minute și {1} secunde", (int)elapsed.TotalMinutes, elapsed.Seconds)
                : F("{0} secunde", Math.Max(1, (int)elapsed.TotalSeconds));
            var result = cancelled
                ? F("Actualizare oprită. {0} din {1} feeduri procesate; {2} reușite, {3} cu eroare, {4} articole noi. Durată: {5}.", _refreshProcessed, _refreshTotal, successes, failures, articlesAdded, duration)
                : F("Actualizare încheiată. {0} feeduri reușite, {1} feeduri cu eroare, {2} articole noi. Durată: {3}.", successes, failures, articlesAdded, duration);
            if (_settings.AutoCleanupEnabled && articlesIgnoredByRetention > 0) result += F(" {0} articole obișnuite mai vechi de {1} zile au fost ignorate conform regulii de păstrare.", articlesIgnoredByRetention, _settings.RetentionDays);
            if (needsAttention > 0)
            {
                var failedAttentionCount = _feeds.Count(feed => feed.ConsecutiveFailures >= 3);
                var silentFeeds = _feeds.Count(feed => feed.HasThreeMonthSilence && feed.ConsecutiveFailures < 3);
                result += F(" {0} feeduri necesită atenție: {1} după 3 erori consecutive și {2} fără articole de peste 3 luni.", needsAttention, failedAttentionCount, silentFeeds);
            }
            if (failureNames.Count > 0) result += F(" Primele feeduri cu eroare: {0}.", string.Join(", ", failureNames.Take(3)));
            if (failureNames.Count > 0) result += UiText.Translate(" Comanda Reîncearcă feedurile cu eroare este disponibilă în meniul Feeduri.");
            Say(result);
            if (!cancelled) SoundAlertService.RefreshFinished(
                _settings.SoundAlertsEnabled,
                _settings.SoundAlertOnSuccess,
                _settings.SoundAlertOnNewArticles,
                _settings.SoundAlertOnErrors,
                failures > 0,
                articlesAdded);
        }
        catch (Exception exception)
        {
            Say(F("Actualizarea nu a putut fi finalizată sau salvată: {0} Datele deja salvate anterior nu au fost șterse.", Describe(exception)));
            SoundAlertService.RefreshFinished(
                _settings.SoundAlertsEnabled,
                _settings.SoundAlertOnSuccess,
                _settings.SoundAlertOnNewArticles,
                _settings.SoundAlertOnErrors,
                hasErrors: true,
                newArticleCount: 0);
        }
        finally
        {
            timer.Stop();
            _isRefreshing = false;
            StopRefreshMenuItem.IsEnabled = false;
            _refreshCancellation?.Dispose();
            _refreshCancellation = null;
            _refreshCompletion?.TrySetResult(true);
            _refreshCompletion = null;
        }
    }
    private void RefreshFeedList(Feed? select = null)
    {
        _suppressFolderFilter = true;
        var priorFolder = SelectedFolder();
        var folders = _feeds.Select(feed => feed.Folder).Where(folder => !string.IsNullOrWhiteSpace(folder)).Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(folder => folder).ToList();
        var folderChoices = folders.Select(folder => new FolderChoice(folder, folder)).Prepend(new FolderChoice(AllFoldersKey, T("Toate folderele"))).ToList();
        FolderFilter.ItemsSource = folderChoices;
        SelectFolder(priorFolder);
        var displayed = DisplayedFeeds(SelectedFolder());
        _suppressFeedSelection = true;
        Feeds.ItemsSource = displayed.OrderBy(feed => feed.Folder).ThenBy(feed => feed.Name).ToList();
        _suppressFeedSelection = false;
        if (select is not null && Feeds.Items.Contains(select)) Feeds.SelectedItem = select;
        _suppressFolderFilter = false;
        UpdateAttentionButton();
    }
    private void RefreshArticleList(Article? select = null, int preferredIndex = -1, bool selectFirstWhenNoMatch = true)
    {
        if (Articles is null || ArticleFilter is null) return;
        var includeSource = _readNowView || _folderAggregateView;
        foreach (var feed in _feeds)
        {
            foreach (var article in feed.Articles)
            {
                article.SourceName = feed.Name;
                article.IncludeSourceInDisplay = includeSource;
            }
        }
        var filter = ArticleFilter.SelectedItem is ComboBoxItem filterItem ? ComboKey(filterItem) : "All";
        IEnumerable<Article> items;
        if (_readNowView)
        {
            var recentArticleSince = DateTimeOffset.Now.AddDays(-Math.Max(1, _settings.ReadNowFavoriteDays));
            items = _feeds.SelectMany(feed => feed.Articles).Where(article =>
                article.ReadLater || article.IsFavorite || article.Published >= recentArticleSince);
        }
        else if (_folderAggregateView)
        {
            var folder = SelectedFolder();
            var relevant = DisplayedFeeds(folder);
            items = relevant.SelectMany(feed => feed.Articles);
        }
        else if (_feed is not null) items = _feed.Articles;
        else { Articles.ItemsSource = null; return; }
        if (filter == "Unread") items = items.Where(article => !article.IsRead);
        if (filter == "Favorites") items = items.Where(article => article.IsFavorite);
        if (filter == "ReadLater") items = items.Where(article => article.ReadLater);
        var tag = TagFilter?.SelectedItem as string ?? "Toate etichetele";
        if (tag != "Toate etichetele") items = items.Where(article => (article.Tags ?? []).Any(articleTag => string.Equals(articleTag, tag, StringComparison.CurrentCultureIgnoreCase)));
        var timeFilter = ArticleTimeFilter?.SelectedItem is ComboBoxItem timeItem ? ComboKey(timeItem) : "Anytime";
        var now = DateTimeOffset.Now;
        if (timeFilter == "Today") items = items.Where(article => article.Published.LocalDateTime.Date == now.LocalDateTime.Date);
        if (timeFilter == "Last24Hours") items = items.Where(article => article.Published >= now.AddHours(-24));
        if (timeFilter == "Last7Days") items = items.Where(article => article.Published >= now.AddDays(-7));
        if (timeFilter == "Last30Days") items = items.Where(article => article.Published >= now.AddDays(-30));
        var query = ArticleSearch?.Text.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(query)) items = items.Where(article => CititorRSS.Jaws.ArticleSearch.Matches(article, query));
        if (_settings.HideRepeatedArticlesInGlobalViews && (_folderAggregateView || _readNowView))
            items = DuplicateCleaner.DistinctForDisplay(items);
        Articles.ItemsSource = items.OrderByDescending(article => article.Published).ToList();
        if (select is not null && Articles.Items.Contains(select)) Articles.SelectedItem = select;
        else if (Articles.Items.Count > 0 && selectFirstWhenNoMatch) Articles.SelectedIndex = preferredIndex >= 0 ? Math.Min(preferredIndex, Articles.Items.Count - 1) : 0;
    }
    private void ArticleFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_restoringSession || Articles is null || Status is null) return;
        _readNowView = false;
        RefreshArticleList();
        Say("Filtru articole aplicat.");
    }
    private void ArticleTimeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_restoringSession || Articles is null || Status is null) return;
        _readNowView = false;
        RefreshArticleList();
        Say("Filtru perioadă articole aplicat.");
    }
    private void ShowUnreadOverview_Click(object sender, RoutedEventArgs e)
    {
        AttentionFilter.IsChecked = false;
        _suppressFolderFilter = true;
        SelectFolder(AllFoldersKey);
        _suppressFolderFilter = false;
        _readNowView = false;
        ArticleFilter.SelectedItem = ArticleFilter.Items.OfType<ComboBoxItem>().First(item => item.Content?.ToString() == "Necitite");
        ArticleTimeFilter.SelectedItem = ArticleTimeFilter.Items.OfType<ComboBoxItem>().First(item => item.Content?.ToString() == "Oricând");
        RefreshTagFilter();
        TagFilter.SelectedItem = "Toate etichetele";
        UpdateArticleFilterSummary();
        ShowFolderAggregate();
        Articles.Focus();
        Say(F("Sunt afișate toate articolele necitite: {0}.", Articles.Items.Count));
    }
    private void ArticleSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressArticleSearch || Articles is null || Status is null) return;
        RefreshArticleList();
        var query = ArticleSearch.Text.Trim();
        if (!string.IsNullOrWhiteSpace(query)) Say(F("Căutare: {0}. {1} articole găsite.", query, Articles.Items.Count));
    }
    private void ArticleSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        ClearArticleSearch();
        e.Handled = true;
    }
    private void ArticleSearch_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        ClearArticleSearch();
        e.Handled = true;
    }
    private void ClearArticleSearch()
    {
        if (string.IsNullOrWhiteSpace(ArticleSearch.Text)) { Articles.Focus(); return; }
        _suppressArticleSearch = true;
        ArticleSearch.Clear();
        _suppressArticleSearch = false;
        RefreshArticleList();
        Say(F("Căutarea a fost ștearsă. {0} articole afișate.", Articles.Items.Count));
        Articles.Focus();
    }
    private void RefreshTagFilter()
    {
        if (TagFilter is null) return;
        var prior = TagFilter.SelectedItem as string ?? "Toate etichetele";
        var tags = _feeds.SelectMany(feed => feed.Articles).SelectMany(article => article.Tags ?? []).Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(tag => tag).ToList();
        tags.Insert(0, "Toate etichetele");
        _suppressTagFilter = true;
        TagFilter.ItemsSource = tags;
        TagFilter.SelectedItem = tags.Contains(prior, StringComparer.CurrentCultureIgnoreCase) ? tags.First(tag => string.Equals(tag, prior, StringComparison.CurrentCultureIgnoreCase)) : "Toate etichetele";
        _suppressTagFilter = false;
        UpdateArticleFilterSummary();
    }
    private void TagFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_restoringSession || _suppressTagFilter || Articles is null || Status is null) return;
        _readNowView = false;
        RefreshArticleList();
        Say("Filtru etichete aplicat.");
    }
    private List<Article> SelectedArticles()
    {
        var selected = Articles.SelectedItems.Cast<Article>().ToList();
        if (selected.Count == 0 && Articles.SelectedItem is Article article) selected.Add(article);
        return selected;
    }

    private List<Article> RelatedArticleCopies(IEnumerable<Article> selected)
    {
        var keys = selected.Select(DuplicateCleaner.ArticleKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return _feeds.SelectMany(feed => feed.Articles)
            .Where(article => keys.Contains(DuplicateCleaner.ArticleKey(article)))
            .Distinct()
            .ToList();
    }
    private sealed record ArticlePosition(List<Article> Items, Article? Anchor, int AnchorIndex, List<Article> Selected);

    private ArticlePosition CaptureArticlePosition()
    {
        var items = Articles.Items.Cast<Article>().ToList();
        var selected = SelectedArticles();
        var anchor = Articles.SelectedItem as Article ?? selected.FirstOrDefault();
        return new ArticlePosition(items, anchor, anchor is null ? -1 : items.IndexOf(anchor), selected);
    }

    private Article? FindDisplayedArticle(Article? candidate)
    {
        if (candidate is null) return null;
        return Articles.Items.Cast<Article>().FirstOrDefault(article =>
            ReferenceEquals(article, candidate) ||
            (!string.IsNullOrWhiteSpace(candidate.Id) && string.Equals(article.Id, candidate.Id, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(candidate.Link) && string.Equals(article.Link, candidate.Link, StringComparison.OrdinalIgnoreCase)));
    }

    private async Task RestoreCapturedPositionAsync(ArticlePosition position, bool keepAnchorWhenVisible = true)
    {
        var selectionRevision = ++_articleSelectionRevision;
        var visibleSelected = position.Selected.Select(FindDisplayedArticle).Where(article => article is not null).Cast<Article>().Distinct().ToList();
        var anchor = keepAnchorWhenVisible ? FindDisplayedArticle(position.Anchor) : null;
        var start = Math.Max(0, position.AnchorIndex + 1);
        var next = position.Items.Skip(start).Select(FindDisplayedArticle).FirstOrDefault(article => article is not null);
        var previous = position.Items.Take(Math.Max(0, position.AnchorIndex)).Reverse().Select(FindDisplayedArticle).FirstOrDefault(article => article is not null);
        var primary = anchor ?? visibleSelected.FirstOrDefault() ?? next ?? previous;
        await RestoreArticleSelectionAsync(visibleSelected, primary, selectionRevision);
        if (primary is null && Articles.Items.Count == 0)
        {
            ArticleFiltersButton.Focus();
            Say("Lista de articole este goală pentru filtrele curente.");
        }
    }

    private async Task ChangeSelectedArticlesAsync(Action<Article> change, string description)
    {
        var selected = SelectedArticles();
        if (selected.Count == 0) { Say("Alege unul sau mai multe articole."); return; }
        var position = CaptureArticlePosition();
        foreach (var article in RelatedArticleCopies(selected)) change(article);
        RefreshArticleList(selectFirstWhenNoMatch: false);
        await _store.SaveAsync(_feeds);
        await RestoreCapturedPositionAsync(position);
        Say(F("{0}: {1} articole. {2}", UiText.Translate(description), selected.Count, ArticlePanelStatus()));
    }
    private async Task RestoreArticleSelectionAsync(IEnumerable<Article> selected, Article? primary, int selectionRevision)
    {
        var selectedItems = selected.ToList();
        await Dispatcher.InvokeAsync(() =>
        {
            if (selectionRevision != _articleSelectionRevision) return;
            Articles.SelectedItems.Clear();
            foreach (var article in selectedItems.Where(article => Articles.Items.Contains(article)))
                Articles.SelectedItems.Add(article);
            if (primary is null || !Articles.Items.Contains(primary)) return;

            Articles.SelectedItem = primary;
            Articles.ScrollIntoView(primary);
            Articles.UpdateLayout();
            if (Articles.ItemContainerGenerator.ContainerFromItem(primary) is ListBoxItem row)
                Keyboard.Focus(row);
            else
                Keyboard.Focus(Articles);
        }, DispatcherPriority.ContextIdle);
    }
    private async void MarkSelectedRead_Click(object sender, RoutedEventArgs e) => await ChangeSelectedArticlesAsync(article => article.IsRead = true, T("Articole marcate citite"));
    private async void MarkSelectedUnread_Click(object sender, RoutedEventArgs e) => await ChangeSelectedArticlesAsync(article => article.IsRead = false, T("Articole marcate necitite"));
    private async void MarkAllDisplayedRead_Click(object sender, RoutedEventArgs e)
    {
        var displayed = Articles.Items.Cast<Article>().Where(article => !article.IsRead).ToList();
        if (displayed.Count == 0)
        {
            Say(T("Nu există articole necitite în lista afișată."));
            return;
        }
        var answer = MessageBox.Show(
            this,
            F("{0} articole afișate vor fi marcate ca citite. Continui?", displayed.Count),
            T("Confirmă marcarea ca citite"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes) return;
        foreach (var article in displayed) article.IsRead = true;
        await _store.SaveAsync(_feeds);
        RefreshArticleList(selectFirstWhenNoMatch: false);
        Say(F("{0}: {1} articole.", T("Articole marcate citite"), displayed.Count));
    }
    private async void AddSelectedFavorites_Click(object sender, RoutedEventArgs e) => await ChangeSelectedArticlesAsync(article => article.IsFavorite = true, T("Articole adăugate la favorite"));
    private async void RemoveSelectedFavorites_Click(object sender, RoutedEventArgs e) => await ChangeSelectedArticlesAsync(article => article.IsFavorite = false, T("Articole eliminate din favorite"));
    private async void AddSelectedReadLater_Click(object sender, RoutedEventArgs e) => await ChangeSelectedArticlesAsync(article => article.ReadLater = true, T("Articole adăugate pentru mai târziu"));
    private async void RemoveSelectedReadLater_Click(object sender, RoutedEventArgs e) => await ChangeSelectedArticlesAsync(article => article.ReadLater = false, T("Articole eliminate din mai târziu"));
    private void CopySelection_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(Reader.SelectedText)) { Say("Nu există text selectat pentru copiere."); return; }
        Clipboard.SetText(Reader.SelectedText); Say("Selecția a fost copiată în clipboard.");
    }
    private void CopyFullArticle_Click(object sender, RoutedEventArgs e)
    {
        if (_article is null) { Say("Alege mai întâi un articol."); return; }
        Clipboard.SetText($"{_article.Title}{Environment.NewLine}{Environment.NewLine}{Reader.Text}"); Say("Articolul complet a fost copiat în clipboard.");
    }
    private void CopyArticleLink_Click(object sender, RoutedEventArgs e)
    {
        if (_article is null || string.IsNullOrWhiteSpace(_article.Link)) { Say("Articolul nu are o adresă care poate fi copiată."); return; }
        Clipboard.SetText(_article.Link); Say("Adresa articolului a fost copiată în clipboard.");
    }
    private void ShareByEmail_Click(object sender, RoutedEventArgs e)
    {
        if (_article is null) { Say("Alege mai întâi un articol."); return; }
        var shareText = ArticleSharing.BuildShareText(_article.Title, Reader.Text, _article.Link);
        var body = ArticleSharing.LimitForUri(shareText, ArticleSharing.EmailBodyLimit);
        var truncated = !string.Equals(body, shareText, StringComparison.Ordinal);
        if (truncated) Clipboard.SetText(shareText);
        var mailto = ArticleSharing.CreateMailto(_article.Title, body);
        try
        {
            Process.Start(new ProcessStartInfo(mailto) { UseShellExecute = true });
            Say(truncated
                ? F("{0} {1}", T("A fost deschisă aplicația de e-mail pentru distribuirea articolului."), T("Articolul complet a fost copiat în clipboard."))
                : T("A fost deschisă aplicația de e-mail pentru distribuirea articolului."));
        }
        catch (Exception exception)
        {
            Say(F("Aplicația de e-mail nu a putut fi deschisă: {0}", Describe(exception)));
            MessageBox.Show(this, F("Aplicația de e-mail nu a putut fi deschisă.\n\n{0}", Describe(exception)), T("Distribuire nereușită"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void ShareByWhatsApp_Click(object sender, RoutedEventArgs e)
    {
        if (_article is null) { Say("Alege mai întâi un articol."); return; }
        var shareText = ArticleSharing.LimitForUri(ArticleSharing.BuildShareText(_article.Title, Reader.Text, _article.Link), ArticleSharing.WhatsAppBodyLimit);
        var address = ArticleSharing.CreateWhatsApp(shareText);
        try
        {
            Process.Start(new ProcessStartInfo(address) { UseShellExecute = true });
            Say(T("WhatsApp a fost deschis pentru distribuirea articolului."));
        }
        catch (Exception exception)
        {
            Say(F("WhatsApp nu a putut fi deschis: {0}", Describe(exception)));
            MessageBox.Show(this, F("WhatsApp nu a putut fi deschis: {0}", Describe(exception)), T("Distribuire nereușită"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void FolderFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_restoringSession || _suppressFolderFilter) return;
        var folder = SelectedFolder();
        ShowFolderAggregate();
        var feedCount = DisplayedFeeds(folder).Count();
        Say(folder == AllFoldersKey
            ? F("Toate folderele: {0} feeduri, {1} articole. Articolele sunt ordonate de la cel mai nou la cel mai vechi.", feedCount, Articles.Items.Count)
            : F("Folder {0}: {1} feeduri, {2} articole. Articolele sunt ordonate de la cel mai nou la cel mai vechi.", folder, feedCount, Articles.Items.Count));
    }
    private void ToggleAttentionFilter_Click(object sender, RoutedEventArgs e)
    {
        AttentionFilter.IsChecked = AttentionFilter.IsChecked != true;
    }

    private void UpdateAttentionButton()
    {
        if (AttentionToggleButton is null || FeedViewSummary is null) return;
        var active = AttentionFilter?.IsChecked == true;
        var count = _feeds.Count(feed => feed.NeedsAttention);
        AttentionToggleButton.Content = active ? T("Revino la toate feedurile") : T("Feeduri care necesită atenție");
        AutomationProperties.SetName(AttentionToggleButton, active ? T("Revino la toate feedurile") : F("Afișează feedurile care necesită atenție. {0} disponibile", count));
        FeedViewSummary.Text = active ? F("Afișare: {0} feeduri care necesită atenție.", count) : T("Afișare: toate feedurile din folderul ales.");
        AutomationProperties.SetName(FeedViewSummary, FeedViewSummary.Text);
    }
    private void AttentionFilter_Changed(object sender, RoutedEventArgs e)
    {
        if (_restoringSession || FolderFilter is null || Feeds is null) return;
        if (AttentionFilter.IsChecked == true)
        {
            _suppressFolderFilter = true;
            SelectFolder(AllFoldersKey);
            _suppressFolderFilter = false;
        }
        FolderFilter.IsEnabled = AttentionFilter.IsChecked != true;
        UpdateAttentionButton();
        ShowFolderAggregate();
        var count = _feeds.Count(feed => feed.NeedsAttention);
        Say(AttentionFilter.IsChecked == true ? F("Sunt afișate toate feedurile care necesită atenție: {0}.", count) : T("Filtrul feedurilor care necesită atenție a fost dezactivat."));
    }
    private void ShowFolderAggregate(bool selectFirstWhenNoMatch = true)
    {
        if (FolderFilter is null || Feeds is null || Articles is null) return;
        var folder = SelectedFolder();
        var displayed = DisplayedFeeds(folder);
        _folderAggregateView = true;
        _feed = null;
        Feeds.ItemsSource = null;
        Feeds.ItemsSource = displayed.OrderBy(feed => feed.Folder).ThenBy(feed => feed.Name).ToList();
        Feeds.SelectedIndex = -1;
        RefreshArticleList(selectFirstWhenNoMatch: selectFirstWhenNoMatch);
    }
    private IEnumerable<Feed> DisplayedFeeds(string folder)
    {
        IEnumerable<Feed> displayed = folder == AllFoldersKey ? _feeds : _feeds.Where(feed => string.Equals(feed.Folder, folder, StringComparison.CurrentCultureIgnoreCase));
        if (AttentionFilter?.IsChecked == true) displayed = displayed.Where(feed => feed.NeedsAttention);
        return displayed;
    }
    private IEnumerable<string> AvailableFolders() => _feeds.Select(feed => feed.Folder);
    private List<Feed> SelectedFeeds()
    {
        var selected = Feeds.SelectedItems.Cast<Feed>().ToList();
        if (selected.Count == 0 && Feeds.SelectedItem is Feed feed) selected.Add(feed);
        return selected;
    }
    private static string Describe(Exception exception) => exception switch
    {
        HttpRequestException => T("Nu s-a putut face conexiunea cu serverul."),
        TaskCanceledException => T("Serverul nu a răspuns în 30 de secunde."),
        InvalidOperationException => T(exception.Message),
        _ => exception.Message
    };
    private bool RejectDataChangeDuringRefresh(string operation)
    {
        if (!_isRefreshing) return false;
        Say(F("Nu se poate începe {0} cât timp feedurile se actualizează. Oprește sau așteaptă încheierea actualizării și încearcă din nou.", UiText.Translate(operation)));
        return true;
    }
    private void SetProgressStatus(string text, bool announce)
    {
        if (announce) { Say(text); return; }
        AutomationProperties.SetLiveSetting(Status, AutomationLiveSetting.Off);
        Status.Text = text;
    }
    private void Say(string text)
    {
        text = UiText.Translate(text);
        StatusAnnouncer.Set(Status, text, ApplicationStatusBar);
        _statusHistory.Add($"{DateTime.Now:HH:mm:ss} — {text}");
        if (_statusHistory.Count > MaximumStatusEntries) _statusHistory.RemoveRange(0, _statusHistory.Count - MaximumStatusEntries);
    }
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);
    private static string T(string source) => UiText.Translate(source);
    private sealed record FolderChoice(string Key, string Label)
    {
        public override string ToString() => Label;
    }
    private static MenuItem? FindFocusedMenuItem(DependencyObject? element)
    {
        while (element is not null)
        {
            if (element is MenuItem item) return item;
            element = System.Windows.Media.VisualTreeHelper.GetParent(element);
        }
        return null;
    }
}
