using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using CititorRSS.Jaws.Localization;
using CititorRSS.Jaws.Services.Content;
using Microsoft.Web.WebView2.Core;

namespace CititorRSS.Jaws;

public partial class ArticleReaderWindow : Window
{
    private readonly Article _article;
    private readonly string _link;
    private readonly AppSettings _settings;
    private readonly Func<Task<string>> _refresh;
    private readonly Func<Task> _saveSettings;
    private readonly Func<Task> _saveArticle;
    private readonly SpeechService _speech;
    private readonly CancellationTokenSource _readerLifetime = new();
    private string? _translationProvider;
    private string? _translationLanguage;
    private string _originalReadableText = string.Empty;
    private bool _isWebViewActive;
    private bool _webViewInitialized;

    public ArticleReaderWindow(Article article, string readableText, AppSettings settings, Func<Task<string>> refresh, Func<Task> saveSettings, SpeechService speech, Func<Task>? saveArticle = null)
    {
        InitializeComponent();
        ArticleText.AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(ArticleText_PreviewKeyDown), true);
        _article = article;
        _link = article.Link;
        _settings = settings;
        _refresh = refresh;
        _saveSettings = saveSettings;
        _speech = speech;
        _saveArticle = saveArticle ?? (() => Task.CompletedTask);
        _speech.StateChanged += SpeechStateChanged;
        LocalizeSaveMenuHeaders(ReaderMenu);
        if (Resources["ReaderContextMenu"] is ItemsControl contextMenu) LocalizeSaveMenuHeaders(contextMenu);
        Width = Math.Max(600, settings.ReaderWindowWidth);
        Height = Math.Max(400, settings.ReaderWindowHeight);
        ArticleText.FontSize = Math.Clamp(settings.ReaderFontSize, 12, 36);
        ApplySpacing(settings.ReaderWideSpacing);
        Title = $"Cititor Orizont — {article.Title}";
        ArticleTitle.Text = article.Title;
        ArticleDetails.Text = string.IsNullOrWhiteSpace(article.SourceName)
            ? article.Published.ToString("dd MMMM yyyy, HH:mm")
            : $"{article.SourceName} · {article.Published:dd MMMM yyyy, HH:mm}";
        ArticleText.SetValue(System.Windows.Automation.AutomationProperties.HelpTextProperty,
            $"{T("Folosește săgețile pentru citire. Shift+F10 deschide comenzile articolului. Ctrl+Shift+F8 comută modul WebReader. Bara de stare anunță încărcarea și acțiunile în curs.")} {T("Tasta Application sau Shift+F10 deschide meniul contextual.")}");
        ArticleWebView.SetValue(System.Windows.Automation.AutomationProperties.HelpTextProperty,
            $"{T("Folosește tastele de navigare web (H pentru titluri, P pentru paragrafe, Tab pentru linkuri). Shift+F10 deschide comenzile articolului. Ctrl+Shift+F8 comută modul text.")} {T("Tasta Application sau Shift+F10 deschide meniul contextual.")}");
        ArticleText.Text = RssReader.CleanReadableContent(readableText, article.Title);
        _originalReadableText = ArticleText.Text;
        SetStatus(T("Articolul este deschis în cititorul Orizont."));

        ShortcutBindings.RefreshMenuHints(this);

        Loaded += async (_, _) =>
        {
            if (string.Equals(_settings.ReaderMode, ReaderModeIds.WebView, StringComparison.OrdinalIgnoreCase))
            {
                await SetReaderModeAsync(ReaderModeIds.WebView, updateSettings: false);
            }
            else
            {
                ArticleText.Focus();
                ArticleText.CaretIndex = 0;
                ArticleText.Select(0, 0);
                UpdateModeMenuText();
            }
        };
    }

    private async Task EnsureWebViewInitializedAsync()
    {
        if (_webViewInitialized) return;

        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CititorRSS-JAWS",
            "WebView2");
        Directory.CreateDirectory(userDataFolder);

        var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
        await ArticleWebView.EnsureCoreWebView2Async(environment);

        ArticleWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        ArticleWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        ArticleWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;

        ArticleWebView.CoreWebView2.ContextMenuRequested += ArticleWebView_ContextMenuRequested;
        ArticleWebView.CoreWebView2.NavigationStarting += ArticleWebView_NavigationStarting;
        _webViewInitialized = true;
    }

    private void ArticleWebView_ContextMenuRequested(object? sender, CoreWebView2ContextMenuRequestedEventArgs e)
    {
        e.Handled = true;
        Dispatcher.InvokeAsync(OpenContextMenu);
    }

    private void ArticleWebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (e.Uri.StartsWith("about:", StringComparison.OrdinalIgnoreCase) ||
            e.Uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        e.Cancel = true;
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri) { UseShellExecute = true });
            SetStatus(T("Linkul a fost deschis în browserul implicit."));
        }
        catch (Exception ex)
        {
            SetStatus(F("Nu s-a putut deschide linkul: {0}", ex.Message));
        }
    }

    private async Task SetReaderModeAsync(string mode, bool updateSettings = true)
    {
        var normalized = ReaderModeIds.Normalize(mode);
        var targetIsWeb = normalized == ReaderModeIds.WebView;

        if (targetIsWeb)
        {
            try
            {
                SetStatus(T("Se încarcă modul WebReader..."));
                await EnsureWebViewInitializedAsync();
                await LoadWebViewContentAsync();
                _isWebViewActive = true;
                ArticleText.Visibility = Visibility.Collapsed;
                ArticleWebView.Visibility = Visibility.Visible;
                ArticleWebView.Focus();
                SetStatus(T("Articolul este afișat în modul WebReader."));
            }
            catch (Exception ex)
            {
                _isWebViewActive = false;
                ArticleWebView.Visibility = Visibility.Collapsed;
                ArticleText.Visibility = Visibility.Visible;
                ArticleText.Focus();
                SetStatus(F("Modul WebReader nu a putut fi inițializat ({0}); se revine la modul text.", ex.Message));
            }
        }
        else
        {
            _isWebViewActive = false;
            ArticleWebView.Visibility = Visibility.Collapsed;
            ArticleText.Visibility = Visibility.Visible;
            ArticleText.Focus();
            SetStatus(T("Articolul este afișat în modul text simplu."));
        }

        UpdateModeMenuText();

        if (updateSettings)
        {
            _settings.ReaderMode = _isWebViewActive ? ReaderModeIds.WebView : ReaderModeIds.Text;
            await _saveSettings();
        }
    }

    private void UpdateModeMenuText()
    {
        if (MenuToggleReaderMode != null)
        {
            MenuToggleReaderMode.Header = _isWebViewActive
                ? T("Comută în modul text")
                : T("Comută în modul WebReader");
        }
    }

    private async void ToggleReaderMode_Click(object sender, RoutedEventArgs e)
    {
        await ToggleReaderModeAsync();
    }

    private async Task ToggleReaderModeAsync()
    {
        var target = _isWebViewActive ? ReaderModeIds.Text : ReaderModeIds.WebView;
        await SetReaderModeAsync(target, updateSettings: true);
    }

    private async Task LoadWebViewContentAsync()
    {
        if (!_webViewInitialized || ArticleWebView.CoreWebView2 == null) return;
        var html = GenerateReaderHtml();
        ArticleWebView.NavigateToString(html);
        await Task.CompletedTask;
    }

    private string GenerateReaderHtml()
    {
        var title = System.Net.WebUtility.HtmlEncode(_article.Title);
        var details = System.Net.WebUtility.HtmlEncode(ArticleDetails.Text);
        var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        var bg = "#ffffff";
        var fg = "#000000";
        var secondary = "#555555";
        var link = "#0b62b2";

        if (Application.Current?.Resources["ThemeWindowBrush"] is SolidColorBrush windowBrush)
            bg = $"#{windowBrush.Color.R:X2}{windowBrush.Color.G:X2}{windowBrush.Color.B:X2}";
        if (Application.Current?.Resources["ThemeWindowTextBrush"] is SolidColorBrush textBrush)
            fg = $"#{textBrush.Color.R:X2}{textBrush.Color.G:X2}{textBrush.Color.B:X2}";
        if (Application.Current?.Resources["ThemeSecondaryTextBrush"] is SolidColorBrush secBrush)
            secondary = $"#{secBrush.Color.R:X2}{secBrush.Color.G:X2}{secBrush.Color.B:X2}";
        if (Application.Current?.Resources["ThemeHighlightBrush"] is SolidColorBrush hlBrush)
            link = $"#{hlBrush.Color.R:X2}{hlBrush.Color.G:X2}{hlBrush.Color.B:X2}";

        var fontSize = Math.Clamp(ArticleText.FontSize, 12, 36);
        var lineHeight = _settings.ReaderWideSpacing ? "2.0" : "1.6";
        var paragraphSpacing = _settings.ReaderWideSpacing ? "1.6em" : "1.2em";

        var content = ArticleText.Text;
        var sb = new System.Text.StringBuilder();
        var paragraphs = content.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in paragraphs)
        {
            var trimmed = p.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;
            sb.Append("<p>").Append(System.Net.WebUtility.HtmlEncode(trimmed)).Append("</p>\n");
        }

        return $@"<!DOCTYPE html>
<html lang=""{lang}"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>{title}</title>
<style>
  body {{
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
    font-size: {fontSize}px;
    line-height: {lineHeight};
    color: {fg};
    background-color: {bg};
    max-width: 52rem;
    margin: 0 auto;
    padding: 1.5rem 1.25rem;
    word-wrap: break-word;
  }}
  h1 {{
    font-size: 1.4em;
    line-height: 1.25;
    margin: 0 0 0.5rem 0;
    color: {fg};
  }}
  .meta {{
    font-size: 0.9em;
    color: {secondary};
    margin-bottom: 1.5rem;
    padding-bottom: 0.5rem;
    border-bottom: 1px solid {secondary};
  }}
  p {{
    margin: 0 0 {paragraphSpacing} 0;
  }}
  a {{
    color: {link};
  }}
  a:focus, a:hover {{
    outline: 2px solid currentColor;
    text-decoration: underline;
  }}
</style>
</head>
<body>
  <h1>{title}</h1>
  <div class=""meta"">{details}</div>
  <article>
    {sb}
  </article>
</body>
</html>";
    }

    private async Task<string> GetSelectedTextAsync()
    {
        if (_isWebViewActive && _webViewInitialized && ArticleWebView.CoreWebView2 != null)
        {
            try
            {
                var json = await ArticleWebView.ExecuteScriptAsync("window.getSelection().toString()");
                if (!string.IsNullOrEmpty(json) && json != "null" && json != "\"\"")
                {
                    return JsonSerializer.Deserialize<string>(json) ?? string.Empty;
                }
            }
            catch { }
            return string.Empty;
        }
        return ArticleText.SelectedText;
    }

    private async Task<string> GetSelectedOrFullTextAsync()
    {
        var selected = await GetSelectedTextAsync();
        return string.IsNullOrWhiteSpace(selected) ? ArticleText.Text : selected;
    }

    private void OpenContextMenu()
    {
        if (Resources["ReaderContextMenu"] is ContextMenu menu)
        {
            menu.PlacementTarget = _isWebViewActive ? (UIElement)ArticleWebView : ArticleText;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Center;
            menu.IsOpen = true;
        }
    }

    private void ArticleText_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = ContextMenuKeyboard.Normalize(e.Key, e.SystemKey);
        // In text mode, let WPF's ContextMenuService handle the Application key natively.
        if (key == Key.Apps) return;
        if (!ContextMenuKeyboard.IsMenuShortcut(key, e.KeyboardDevice.Modifiers)) return;
        if (Resources["ReaderContextMenu"] is not ContextMenu menu || !menu.IsOpen) OpenContextMenu();
        e.Handled = true;
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetStatus(T("Se pregătește articolul în cititorul Orizont."));
            ArticleText.Text = RssReader.CleanReadableContent(await _refresh(), _article.Title);
            _originalReadableText = ArticleText.Text;
            _translationProvider = null;
            _translationLanguage = null;
            if (_isWebViewActive)
            {
                await LoadWebViewContentAsync();
            }
            else
            {
                ArticleText.Focus(); ArticleText.CaretIndex = 0; ArticleText.Select(0, 0);
            }
            SetStatus(T("Articolul este deschis în cititorul Orizont."));
        }
        catch (Exception exception)
        {
            var message = F("Articolul nu a putut fi reîncărcat: {0}", exception.Message);
            SetStatus(message);
            MessageBox.Show(this, message, T("Deschidere nereușită"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void IncreaseText_Click(object sender, RoutedEventArgs e) => SetFontSize(ArticleText.FontSize + 2);
    private void DecreaseText_Click(object sender, RoutedEventArgs e) => SetFontSize(ArticleText.FontSize - 2);
    private void NormalSpacing_Click(object sender, RoutedEventArgs e) => ApplySpacing(false);
    private void WideSpacing_Click(object sender, RoutedEventArgs e) => ApplySpacing(true);

    private void SetFontSize(double size)
    {
        ArticleText.FontSize = Math.Clamp(size, 12, 36);
        _settings.ReaderFontSize = ArticleText.FontSize;
        if (_isWebViewActive) _ = LoadWebViewContentAsync();
    }

    private void ApplySpacing(bool wide)
    {
        ArticleText.Padding = wide ? new Thickness(0, 8, 0, 8) : new Thickness(0);
        _settings.ReaderWideSpacing = wide;
        if (_isWebViewActive) _ = LoadWebViewContentAsync();
    }

    private async void Window_Closed(object? sender, EventArgs e)
    {
        _readerLifetime.Cancel();
        _speech.StateChanged -= SpeechStateChanged;
        _settings.ReaderWindowWidth = Width;
        _settings.ReaderWindowHeight = Height;
        _settings.ReaderFontSize = ArticleText.FontSize;
        _settings.ReaderMode = _isWebViewActive ? ReaderModeIds.WebView : ReaderModeIds.Text;
        await _saveSettings();
    }

    private void SpeechStateChanged(string message)
    {
        Dispatcher.Invoke(() => SetStatus(message));
    }

    private void SetStatus(string message) => StatusAnnouncer.Set(Status, message, ReaderStatusBar);

    private void Content_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (ArticleText.Text.Length == 0) e.Handled = true;
    }

    private async void AiSummarize_Click(object sender, RoutedEventArgs e) => await AskAiAsync(T("Rezumat"), T("Rezuma articolul în limba interfeței. Evidențiază faptele, contextul și incertitudinile."));
    private async void AiTranslate_Click(object sender, RoutedEventArgs e) => await AskAiAsync(T("Traducere"), T("Tradu articolul complet în limba interfeței. Nu adăuga comentarii, păstrează structura paragrafului."), isTranslation: true);
    private async void AiExplain_Click(object sender, RoutedEventArgs e) => await AskAiAsync(T("Explicație"), T("Explică articolul în limba interfeței, simplu și clar, pentru un cititor nespecialist."));
    private async void AiKeyPoints_Click(object sender, RoutedEventArgs e) => await AskAiAsync(T("Ideile principale"), T("Extrage ideile principale ale articolului într-o listă clară, în limba interfeței."));
    private async void AiDiscuss_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AiQuestionWindow(AiProviderIds.DisplayName(_settings.AiDefaultProvider)) { Owner = this };
        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.UserQuestion)) return;
        await AskAiAsync(T("Răspuns AI"), dialog.UserQuestion);
    }

    private async Task AskAiAsync(string title, string request, bool isTranslation = false)
    {
        var provider = AiProviderIds.Normalize(_settings.AiDefaultProvider);
        var providerName = AiProviderIds.DisplayName(provider);
        var enabled = provider switch
        {
            AiProviderIds.OpenAI => _settings.OpenAiEnabled,
            AiProviderIds.Mistral => _settings.MistralEnabled,
            AiProviderIds.DeepSeek => _settings.DeepSeekEnabled,
            _ => _settings.GeminiEnabled
        };
        if (!enabled)
        {
            ShowAiProblem(F("Furnizorul AI implicit nu este activat. Deschide Setări Inteligență artificială și verifică cheia API pentru {0}.", providerName));
            return;
        }
        string key;
        var encryptedKey = provider switch
        {
            AiProviderIds.OpenAI => _settings.EncryptedOpenAiKey,
            AiProviderIds.Mistral => _settings.EncryptedMistralKey,
            AiProviderIds.DeepSeek => _settings.EncryptedDeepSeekKey,
            _ => _settings.EncryptedGeminiKey
        };
        try { key = SecretProtector.Unprotect(encryptedKey); }
        catch
        {
            ShowAiProblem(F("Cheia API pentru {0} nu poate fi citită pentru acest cont Windows. Introdu cheia din nou în Setări Inteligență artificială.", providerName));
            return;
        }
        if (string.IsNullOrWhiteSpace(key))
        {
            ShowAiProblem(F("Nu există o cheie API pentru {0}. Deschide Setări Inteligență artificială.", providerName));
            return;
        }
        var articleText = await GetSelectedOrFullTextAsync();
        if (string.IsNullOrWhiteSpace(articleText))
        {
            ShowAiProblem(T("Articolul nu conține text pentru furnizorul AI selectat. Încearcă să aduci textul complet."));
            return;
        }
        var prompt = $"Limba interfeței: {CultureInfo.CurrentUICulture.NativeName}\nTitlu articol: {_article.Title}\nSursă: {_article.Link}\n\nCerere: {request}\n\nArticol:\n{articleText}";
        var model = provider switch
        {
            AiProviderIds.Mistral => _settings.MistralModel,
            AiProviderIds.DeepSeek => _settings.DeepSeekModel,
            _ => _settings.OpenAiModel
        };
        try
        {
            SetStatus(T("Se pregătește articolul în cititorul Orizont."));
            var response = await new AiProviderService().GenerateAsync(provider, key, model, _settings.AiInstructions, prompt);
            var responseWindow = CreateAiResponseWindow(
                title,
                response,
                providerName,
                isTranslation,
                followUp => new AiProviderService().GenerateAsync(provider, key, model, _settings.AiInstructions, $"{prompt}\n\n{followUp}"),
                async noteContent =>
                {
                    _article.AiNotes ??= [];
                    _article.AiNotes.Add(new AiNote { Title = title, Content = noteContent });
                    await _saveArticle();
                },
                articleText,
                isTranslation ? CultureInfo.CurrentUICulture.NativeName : null);
            responseWindow.Owner = this;
            responseWindow.ShowDialog();
        }
        catch (TaskCanceledException) { var message = F("{0} nu a răspuns în 45 de secunde. Încearcă din nou mai târziu.", providerName); SetStatus(message); ShowAiProblem(message); }
        catch (HttpRequestException) { var message = F("Nu s-a putut ajunge la {0}. Verifică internetul, firewall-ul sau proxy-ul.", providerName); SetStatus(message); ShowAiProblem(message); }
        catch (Exception exception) { var message = F("Furnizorul {0} nu a putut executa comanda {1}. Detalii: {2}", providerName, title, exception.Message); SetStatus(message); ShowAiProblem(message); }
    }

    private AiResponseWindow CreateAiResponseWindow(
        string title,
        string response,
        string providerName,
        bool isTranslation,
        Func<string, Task<string>> continueConversation,
        Func<string, Task> saveNote,
        string sourceArticleText,
        string? translationLanguage)
        => new(
            title,
            response,
            _article.Title,
            _article.Link,
            continueConversation,
            saveNote,
            _speech,
            providerName,
            isTranslation,
            sourceArticleText,
            translationLanguage);

    private async void DeepLTranslate_Click(object sender, RoutedEventArgs e)
    {
        if (!_settings.DeepLEnabled) { ShowAiProblem(T("DeepL nu este activat. Deschide Setări, Setări Inteligență artificială, testează cheia DeepL și apasă Salvează.")); return; }
        string key;
        try { key = SecretProtector.Unprotect(_settings.EncryptedDeepLKey); }
        catch { ShowAiProblem(T("Cheia DeepL salvată nu poate fi citită pentru acest cont Windows. Introdu cheia din nou în Setări Inteligență artificială.")); return; }
        if (string.IsNullOrWhiteSpace(key)) { ShowAiProblem(T("Nu există o cheie DeepL salvată. Deschide Setări Inteligență artificială.")); return; }
        try
        {
            var source = await ArticleTranslationSource.ResolveAsync(_article.FullContent, _originalReadableText, _refresh, _article.Title);
            if (string.IsNullOrWhiteSpace(source)) { ShowAiProblem(T("Articolul nu conține text pentru traducere.")); return; }
            SetStatus(T("Se traduce articolul cu DeepL în limba interfeței. Așteaptă."));
            var targetLanguage = DeepLConnection.TargetLanguageForUi();
            var translated = await new DeepLConnection().TranslateAsync(key, source, targetLanguage);
            ArticleText.Text = translated;
            _translationProvider = "DeepL";
            _translationLanguage = ArticleDistributionContext.LanguageNameForCode(targetLanguage);
            if (_isWebViewActive)
            {
                await LoadWebViewContentAsync();
            }
            else
            {
                ArticleText.Focus();
                ArticleText.CaretIndex = 0;
                ArticleText.Select(0, 0);
            }
            SetStatus(T("Articolul a fost tradus cu DeepL."));
        }
        catch (Exception exception) { var message = F("DeepL nu a putut traduce articolul: {0}", exception.Message); SetStatus(message); ShowAiProblem(message); }
    }

    private async void GoogleTranslate_Click(object sender, RoutedEventArgs e)
    {
        if (_readerLifetime.IsCancellationRequested) return;
        using var requestLease = GoogleTranslateRequestGate.TryAcquire();
        if (requestLease is null)
        {
            SetStatus(T("Se traduce articolul cu Google Translate. Așteaptă."));
            return;
        }

        try
        {
            var cancellationToken = _readerLifetime.Token;
            var selectedText = await GetSelectedTextAsync();
            cancellationToken.ThrowIfCancellationRequested();
            var textToTranslate = string.IsNullOrWhiteSpace(selectedText) ? _originalReadableText : selectedText;
            if (string.IsNullOrWhiteSpace(textToTranslate))
            {
                MessageBox.Show(this, T("Articolul nu conține text pentru traducere."), T("Traducere Google Translate"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirmation = MessageBox.Show(
                this,
                T("Google Translate folosește un serviciu online neoficial care se poate schimba sau limita. Textul integral al articolului va fi trimis către Google. Continui?"),
                T("Traducere Google Translate"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            if (confirmation != MessageBoxResult.Yes)
            {
                SetStatus(T("Traducerea Google Translate a fost anulată."));
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var selectionStart = ArticleText.SelectionStart;
            var selectionLength = ArticleText.SelectionLength;
            SetStatus(T("Se traduce articolul cu Google Translate. Așteaptă."));
            var source = await ArticleTranslationSource.ResolveAsync(_article.FullContent, textToTranslate, _refresh, _article.Title);
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(source))
            {
                MessageBox.Show(this, T("Articolul nu conține text pentru traducere."), T("Traducere Google Translate"), MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var targetLanguage = GoogleTranslateConnection.ResolveTargetLanguage(_settings.GoogleTranslateTargetLanguage);
            var translated = await new GoogleTranslateConnection().TranslateAsync(
                source,
                targetLanguage,
                GoogleTranslateConnection.NormalizeSourceLanguage(_settings.GoogleTranslateSourceLanguage),
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            requestLease.Dispose();
            var resultWindow = new ArticleTranslationWindow(translated, _article.Title, _link, "Google Translate", source,
                ArticleDistributionContext.LanguageNameForCode(targetLanguage)) { Owner = this };
            resultWindow.ShowDialog();
            if (!_readerLifetime.IsCancellationRequested)
            {
                if (!_isWebViewActive)
                {
                    ArticleText.Focus();
                    ArticleText.Select(selectionStart, selectionLength);
                }
                SetStatus(T("Traducerea Google Translate s-a încheiat. Rezultatul este într-o fereastră separată."));
            }
        }
        catch (OperationCanceledException) when (_readerLifetime.IsCancellationRequested)
        {
        }
        catch (Exception) when (_readerLifetime.IsCancellationRequested)
        {
        }
        catch (TaskCanceledException)
        {
            var message = T("Google Translate nu a răspuns în 45 de secunde. Încearcă din nou mai târziu.");
            SetStatus(message);
            MessageBox.Show(this, message, T("Traducere Google Translate"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (HttpRequestException)
        {
            var message = T("Nu s-a putut ajunge la Google Translate. Verifică internetul și încearcă din nou.");
            SetStatus(message);
            MessageBox.Show(this, message, T("Traducere Google Translate"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (InvalidOperationException exception)
        {
            SetStatus(exception.Message);
            MessageBox.Show(this, exception.Message, T("Traducere Google Translate"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception exception)
        {
            var message = F("Google Translate nu a putut traduce articolul: {0}", exception.Message);
            SetStatus(message);
            MessageBox.Show(this, message, T("Traducere Google Translate"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ShowAiNotes_Click(object sender, RoutedEventArgs e)
    {
        if (_article.AiNotes is not { Count: > 0 })
        {
            MessageBox.Show(this, T("Acest articol nu are încă notițe AI salvate."), T("Notițe AI"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        new AiNotesWindow(_article) { Owner = this }.ShowDialog();
    }

    private void ShowAiProblem(string message)
    {
        MessageBox.Show(this, message, T("AI — stare comandă"), MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private static string T(string source) => UiText.Translate(source);
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);

    private void LocalizeSaveMenuHeaders(ItemsControl root)
    {
        string[] keys = ["Salvează articolul", "Salvează ca TXT", "Salvează ca RTF"];
        foreach (var item in root.Items.OfType<MenuItem>())
        {
            if (item.Header is string header && keys.Contains(header, StringComparer.Ordinal)) item.Header = T(header);
            LocalizeSaveMenuHeaders(item);
        }
    }

    private void SaveArticleAsTxt_Click(object sender, RoutedEventArgs e) => SaveArticleWithFormat("txt");

    private void SaveArticleAsRtf_Click(object sender, RoutedEventArgs e) => SaveArticleWithFormat("rtf");

    private void SaveArticleWithFormat(string format)
    {
        var context = CreateArticleDistributionContext(ArticleText.Text);
        var translationNote = context.BuildTranslationNote(T("Traducere automată realizată prin {0} în limba {1}."));
        try
        {
            var path = ArticleExportService.Save(_article, context, _settings.ArticleExportFolder, _article.SourceName, format,
                T("Conținut preluat prin Orizont RSS:"), T("Sursa articolului:"), translationNote);
            SetStatus(F("Articol salvat: {0}", path));
        }
        catch (Exception exception)
        {
            SetStatus(F("Articolul nu a putut fi salvat: {0}", exception.Message));
        }
    }

    private async void Window_KeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (ShortcutBindings.Matches("ReaderMode", key, Keyboard.Modifiers))
        {
            await ToggleReaderModeAsync();
            e.Handled = true;
            return;
        }
        if (ContextMenuKeyboard.ShouldHandleReaderWindowShortcut(key, Keyboard.Modifiers, _isWebViewActive))
        {
            OpenContextMenu();
            e.Handled = true;
            return;
        }
        if (ShortcutBindings.Matches("VoiceToggle", key, Keyboard.Modifiers))
        {
            if (_speech.IsSpeaking || _speech.IsPaused) PauseResumeSpeech_Click(this, e);
            else SpeakContent_Click(this, e);
            e.Handled = true;
            return;
        }
        if (ShortcutBindings.Matches("Window", key, Keyboard.Modifiers))
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            var message = WindowState == WindowState.Maximized
                ? T("Fereastra a fost maximizată.")
                : T("Fereastra a fost restabilită.");
            SetStatus(message);
            e.Handled = true;
            return;
        }
        if (e.Key != Key.Escape) return;
        if (_speech.IsSpeaking || _speech.IsPaused)
        {
            _speech.Stop();
            e.Handled = true;
            return;
        }
        Close();
        e.Handled = true;
        return;
    }

    private async void SpeakContent_Click(object sender, RoutedEventArgs e)
    {
        if (!_speech.EnsureAvailable()) { SetStatus(T("Motorul vocal selectat nu este disponibil. Verifică motorul și vocea în Setări.")); return; }
        var selected = await GetSelectedTextAsync();
        var text = !string.IsNullOrWhiteSpace(selected) ? selected : ArticleSpeechDocument();
        if (string.IsNullOrWhiteSpace(text)) { SetStatus(T("Nu există conținut de citit cu voce.")); return; }
        _speech.Speak(text);
    }

    private void SpeakFromCursor_Click(object sender, RoutedEventArgs e)
    {
        if (!_speech.EnsureAvailable()) { SetStatus(T("Motorul vocal selectat nu este disponibil. Verifică motorul și vocea în Setări.")); return; }
        var start = Math.Clamp(ArticleText.CaretIndex, 0, ArticleText.Text.Length);
        var text = ArticleText.Text[start..];
        if (string.IsNullOrWhiteSpace(text)) { SetStatus(T("Cursorul se află la sfârșitul conținutului.")); return; }
        _speech.Speak(text);
    }

    private void PauseResumeSpeech_Click(object sender, RoutedEventArgs e)
    {
        if (!_speech.EnsureAvailable() || !_speech.PauseOrResume())
            SetStatus(T("Nu există o citire vocală în curs pentru pauză sau continuare."));
    }

    private void StopSpeech_Click(object sender, RoutedEventArgs e)
    {
        if (!_speech.IsSpeaking && !_speech.IsPaused) { SetStatus(T("Nu există o citire vocală în curs.")); return; }
        _speech.Stop();
    }

    private string ArticleSpeechDocument() => F("Titlu: {0}.{1}Data publicării: {2}.{1}{1}{3}", _article.Title, Environment.NewLine, _article.Published.ToString("f", CultureInfo.CurrentCulture), ArticleText.Text);

    private ArticleDistributionContext CreateArticleDistributionContext(string displayText) => new(
        _article.Title,
        _originalReadableText,
        displayText,
        _link,
        _translationProvider,
        _translationLanguage);

    private async void CopySelection_Click(object sender, RoutedEventArgs e)
    {
        var selected = await GetSelectedTextAsync();
        if (!string.IsNullOrEmpty(selected)) Clipboard.SetText(selected);
    }

    private void CopyFullArticle_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(ArticleText.Text)) return;
        var context = CreateArticleDistributionContext(ArticleText.Text);
        var note = context.BuildTranslationNote(T("Traducere automată realizată prin {0} în limba {1}."));
        Clipboard.SetText(ArticleSharing.BuildShareText(context, note, T("Conținut preluat prin Orizont RSS:"), T("Sursa articolului:")));
        SetStatus(T("Articolul complet a fost copiat în clipboard."));
    }

    private void CopyLink_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_link)) Clipboard.SetText(_link);
    }

    private void ShareByEmail_Click(object sender, RoutedEventArgs e)
    {
        var context = CreateArticleDistributionContext(ArticleText.Text);
        var translationNote = context.BuildTranslationNote(T("Traducere automată realizată prin {0} în limba {1}."));
        var footer = ArticleSharing.BuildFooter(context, translationNote, T("Conținut preluat prin Orizont RSS:"), T("Sursa articolului:"));
        var shareText = ArticleSharing.BuildShareText(context, translationNote, T("Conținut preluat prin Orizont RSS:"), T("Sursa articolului:"));
        var body = ArticleSharing.LimitForUri(shareText, ArticleSharing.EmailBodyLimit, footer);
        var truncated = !string.Equals(body, shareText, StringComparison.Ordinal);
        if (truncated) Clipboard.SetText(shareText);
        var mailto = ArticleSharing.CreateMailto(_article.Title, body);
        try
        {
            Process.Start(new ProcessStartInfo(mailto) { UseShellExecute = true });
            SetStatus(truncated
                ? F("{0} {1}", T("A fost deschisă aplicația de e-mail pentru distribuirea articolului."), T("Articolul complet a fost copiat în clipboard."))
                : T("A fost deschisă aplicația de e-mail pentru distribuirea articolului."));
        }
        catch (Exception exception)
        {
            var message = F("Aplicația de e-mail nu a putut fi deschisă: {0}", exception.Message);
            SetStatus(message);
            MessageBox.Show(this, message, T("Distribuire nereușită"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShareByWhatsApp_Click(object sender, RoutedEventArgs e)
    {
        var context = CreateArticleDistributionContext(ArticleText.Text);
        var translationNote = context.BuildTranslationNote(T("Traducere automată realizată prin {0} în limba {1}."));
        var footer = ArticleSharing.BuildFooter(context, translationNote, T("Conținut preluat prin Orizont RSS:"), T("Sursa articolului:"));
        var fullShareText = ArticleSharing.BuildShareText(context, translationNote, T("Conținut preluat prin Orizont RSS:"), T("Sursa articolului:"));
        var shareText = ArticleSharing.LimitForUri(fullShareText, ArticleSharing.WhatsAppBodyLimit, footer);
        var truncated = !string.Equals(shareText, fullShareText, StringComparison.Ordinal);
        if (truncated) Clipboard.SetText(fullShareText);
        var address = ArticleSharing.CreateWhatsApp(shareText);
        try
        {
            Process.Start(new ProcessStartInfo(address) { UseShellExecute = true });
            SetStatus(truncated
                ? F("{0} {1}", T("WhatsApp a fost deschis pentru distribuirea articolului."), T("Articolul complet a fost copiat în clipboard."))
                : T("WhatsApp a fost deschis pentru distribuirea articolului."));
        }
        catch (Exception exception)
        {
            var message = F("WhatsApp nu a putut fi deschis: {0}", exception.Message);
            SetStatus(message);
            MessageBox.Show(this, message, T("Distribuire nereușită"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
