using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Globalization;
using System.Net.Http;
using CititorRSS.Jaws.Localization;

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

    public ArticleReaderWindow(Article article, string readableText, AppSettings settings, Func<Task<string>> refresh, Func<Task> saveSettings, SpeechService speech, Func<Task>? saveArticle = null)
    {
        InitializeComponent();
        _article = article;
        _link = article.Link;
        _settings = settings;
        _refresh = refresh;
        _saveSettings = saveSettings;
        _speech = speech;
        _saveArticle = saveArticle ?? (() => Task.CompletedTask);
        _speech.StateChanged += SpeechStateChanged;
        Width = Math.Max(600, settings.ReaderWindowWidth);
        Height = Math.Max(400, settings.ReaderWindowHeight);
        ArticleText.FontSize = Math.Clamp(settings.ReaderFontSize, 12, 36);
        ApplySpacing(settings.ReaderWideSpacing);
        Title = $"Cititor Orizont — {article.Title}";
        ArticleTitle.Text = article.Title;
        ArticleDetails.Text = string.IsNullOrWhiteSpace(article.SourceName)
            ? article.Published.ToString("dd MMMM yyyy, HH:mm")
            : $"{article.SourceName} · {article.Published:dd MMMM yyyy, HH:mm}";
        ArticleText.Text = readableText;
        Status.Text = T("Articolul este deschis în cititorul Orizont.");
        Loaded += (_, _) => { ArticleText.Focus(); ArticleText.CaretIndex = 0; ArticleText.Select(0, 0); };
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Status.Text = T("Se pregătește articolul în cititorul Orizont.");
            ArticleText.Text = await _refresh();
            ArticleText.Focus(); ArticleText.CaretIndex = 0; ArticleText.Select(0, 0);
            Status.Text = T("Articolul este deschis în cititorul Orizont.");
        }
        catch (Exception exception)
        {
            Status.Text = F("Articolul nu a putut fi reîncărcat: {0}", exception.Message);
            MessageBox.Show(this, $"Articolul nu a putut fi reîncărcat.\n\n{exception.Message}", "Reîncărcare nereușită", MessageBoxButton.OK, MessageBoxImage.Warning);
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
    }

    private void ApplySpacing(bool wide)
    {
        // WPF TextBox nu expune spațierea rândurilor; paddingul vertical oferă un
        // mod de lectură mai aerisit fără să modifice textul copiat.
        ArticleText.Padding = wide ? new Thickness(0, 8, 0, 8) : new Thickness(0);
        _settings.ReaderWideSpacing = wide;
    }

    private async void Window_Closed(object? sender, EventArgs e)
    {
        _speech.StateChanged -= SpeechStateChanged;
        _settings.ReaderWindowWidth = Width;
        _settings.ReaderWindowHeight = Height;
        _settings.ReaderFontSize = ArticleText.FontSize;
        await _saveSettings();
    }

    private void SpeechStateChanged(string message)
    {
        Dispatcher.Invoke(() => StatusAnnouncer.Set(Status, message, ReaderStatusBar));
    }

    private void Content_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (ArticleText.Text.Length == 0) e.Handled = true;
    }

    private async void AiSummarize_Click(object sender, RoutedEventArgs e) => await AskGeminiAsync(T("Rezumat"), T("Rezuma articolul în limba interfeței. Evidențiază faptele, contextul și incertitudinile."));
    private async void AiTranslate_Click(object sender, RoutedEventArgs e) => await AskGeminiAsync(T("Traducere"), T("Tradu articolul complet în limba interfeței. Nu adăuga comentarii, păstrează structura paragrafului."));
    private async void AiExplain_Click(object sender, RoutedEventArgs e) => await AskGeminiAsync(T("Explicație"), T("Explică articolul în limba interfeței, simplu și clar, pentru un cititor nespecialist."));
    private async void AiKeyPoints_Click(object sender, RoutedEventArgs e) => await AskGeminiAsync(T("Ideile principale"), T("Extrage ideile principale ale articolului într-o listă clară, în limba interfeței."));
    private async void AiDiscuss_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AiQuestionWindow { Owner = this };
        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.UserQuestion)) return;
        await AskGeminiAsync(T("Răspuns Gemini"), dialog.UserQuestion);
    }

    private async Task AskGeminiAsync(string title, string request)
    {
        if (!_settings.GeminiEnabled)
        {
            ShowAiProblem(T("Gemini nu este activat. Deschide Setări, Setări Inteligență artificială, testează cheia Gemini și apasă Salvează."));
            return;
        }
        string key;
        try { key = SecretProtector.Unprotect(_settings.EncryptedGeminiKey); }
        catch
        {
            ShowAiProblem(T("Cheia Gemini salvată nu poate fi citită pentru acest cont Windows. Introdu cheia din nou în Setări Inteligență artificială."));
            return;
        }
        if (string.IsNullOrWhiteSpace(key))
        {
            ShowAiProblem(T("Nu există o cheie Gemini salvată. Deschide Setări Inteligență artificială."));
            return;
        }
        if (string.IsNullOrWhiteSpace(ArticleText.Text))
        {
            ShowAiProblem(T("Articolul nu conține text pentru Gemini. Reîncarcă articolul și încearcă din nou."));
            return;
        }
        var prompt = $"Limba interfeței: {CultureInfo.CurrentUICulture.NativeName}\nTitlu articol: {_article.Title}\nSursă: {_article.Link}\n\nCerere: {request}\n\nArticol:\n{ArticleText.Text}";
        try
        {
            Status.Text = T("Se pregătește articolul în cititorul Orizont.");
            var response = await new GeminiConnection().GenerateAsync(key, _settings.AiInstructions, prompt);
            new AiResponseWindow(
                title,
                response,
                _article.Title,
                _article.Link,
                followUp => new GeminiConnection().GenerateAsync(key, _settings.AiInstructions, $"{prompt}\n\n{followUp}"),
                async noteContent =>
                {
                    _article.AiNotes ??= [];
                    _article.AiNotes.Add(new AiNote { Title = title, Content = noteContent });
                    await _saveArticle();
                },
                null) { Owner = this }.ShowDialog();
        }
        catch (TaskCanceledException) { Status.Text = T("Gemini nu a răspuns în 45 de secunde. Încearcă din nou mai târziu."); ShowAiProblem(Status.Text); }
        catch (HttpRequestException) { Status.Text = T("Nu s-a putut ajunge la Gemini. Verifică internetul, firewall-ul sau proxy-ul."); ShowAiProblem(Status.Text); }
        catch (Exception exception) { Status.Text = F("Gemini nu a putut executa comanda „{0}”. Detalii: {1}", title, exception.Message); ShowAiProblem(Status.Text); }
    }

    private async void DeepLTranslate_Click(object sender, RoutedEventArgs e)
    {
        if (!_settings.DeepLEnabled) { ShowAiProblem(T("DeepL nu este activat. Deschide Setări, Setări Inteligență artificială, testează cheia DeepL și apasă Salvează.")); return; }
        string key;
        try { key = SecretProtector.Unprotect(_settings.EncryptedDeepLKey); }
        catch { ShowAiProblem(T("Cheia DeepL salvată nu poate fi citită pentru acest cont Windows. Introdu cheia din nou în Setări Inteligență artificială.")); return; }
        if (string.IsNullOrWhiteSpace(key)) { ShowAiProblem(T("Nu există o cheie DeepL salvată. Deschide Setări Inteligență artificială.")); return; }
        if (string.IsNullOrWhiteSpace(ArticleText.Text)) { ShowAiProblem(T("Articolul nu conține text pentru traducere.")); return; }
        try
        {
            Status.Text = T("Se traduce articolul cu DeepL în limba interfeței. Așteaptă.");
            var translated = await new DeepLConnection().TranslateAsync(key, ArticleText.Text, DeepLConnection.TargetLanguageForUi());
            ArticleText.Text = translated;
            ArticleText.Focus();
            ArticleText.CaretIndex = 0;
            ArticleText.Select(0, 0);
            Status.Text = T("Articolul a fost tradus cu DeepL.");
        }
        catch (Exception exception) { Status.Text = F("DeepL nu a putut traduce articolul: {0}", exception.Message); ShowAiProblem(Status.Text); }
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

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.F9 && Keyboard.Modifiers == ModifierKeys.None)
        {
            if (_speech.IsSpeaking || _speech.IsPaused) PauseResumeSpeech_Click(this, e);
            else SpeakContent_Click(this, e);
            e.Handled = true;
            return;
        }
        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt))
        {
            if (key == Key.V) SpeakContent_Click(this, e);
            else if (key == Key.P) PauseResumeSpeech_Click(this, e);
            else if (key == Key.S) StopSpeech_Click(this, e);
            else goto ContinueWindowKeys;
            e.Handled = true;
            return;
        }
        if (e.Key == Key.F11 && Keyboard.Modifiers == ModifierKeys.None)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            var message = WindowState == WindowState.Maximized
                ? T("Fereastra a fost maximizată.")
                : T("Fereastra a fost restabilită.");
            StatusAnnouncer.Set(Status, message, ReaderStatusBar);
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

    ContinueWindowKeys:;
    }

    private void SpeakContent_Click(object sender, RoutedEventArgs e)
    {
        if (!_speech.EnsureAvailable()) { StatusAnnouncer.Set(Status, T("Motorul vocal selectat nu este disponibil. Verifică motorul și vocea în Setări."), ReaderStatusBar); return; }
        var text = ArticleText.SelectionLength > 0 ? ArticleText.SelectedText : ArticleSpeechDocument();
        if (string.IsNullOrWhiteSpace(text)) { StatusAnnouncer.Set(Status, T("Nu există conținut de citit cu voce."), ReaderStatusBar); return; }
        _speech.Speak(text);
    }

    private void SpeakFromCursor_Click(object sender, RoutedEventArgs e)
    {
        if (!_speech.EnsureAvailable()) { StatusAnnouncer.Set(Status, T("Motorul vocal selectat nu este disponibil. Verifică motorul și vocea în Setări."), ReaderStatusBar); return; }
        var start = Math.Clamp(ArticleText.CaretIndex, 0, ArticleText.Text.Length);
        var text = ArticleText.Text[start..];
        if (string.IsNullOrWhiteSpace(text)) { StatusAnnouncer.Set(Status, T("Cursorul se află la sfârșitul conținutului."), ReaderStatusBar); return; }
        _speech.Speak(text);
    }

    private void PauseResumeSpeech_Click(object sender, RoutedEventArgs e)
    {
        if (!_speech.EnsureAvailable() || !_speech.PauseOrResume())
            StatusAnnouncer.Set(Status, T("Nu există o citire vocală în curs pentru pauză sau continuare."), ReaderStatusBar);
    }

    private void StopSpeech_Click(object sender, RoutedEventArgs e)
    {
        if (!_speech.IsSpeaking && !_speech.IsPaused) { StatusAnnouncer.Set(Status, T("Nu există o citire vocală în curs."), ReaderStatusBar); return; }
        _speech.Stop();
    }

    private string ArticleSpeechDocument() => F("Titlu: {0}.{1}Data publicării: {2}.{1}{1}{3}", _article.Title, Environment.NewLine, _article.Published.ToString("f", CultureInfo.CurrentCulture), ArticleText.Text);

    private void CopySelection_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(ArticleText.SelectedText)) return;
        Clipboard.SetText(ArticleText.SelectedText);
    }

    private void CopyFullArticle_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(ArticleText.Text)) Clipboard.SetText(ArticleText.Text);
    }

    private void CopyLink_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_link)) Clipboard.SetText(_link);
    }

    private void ShareByEmail_Click(object sender, RoutedEventArgs e)
    {
        var shareText = ArticleSharing.BuildShareText(_article.Title, ArticleText.Text, _link);
        var body = ArticleSharing.LimitForUri(shareText, ArticleSharing.EmailBodyLimit);
        var truncated = !string.Equals(body, shareText, StringComparison.Ordinal);
        if (truncated) Clipboard.SetText(shareText);
        var mailto = ArticleSharing.CreateMailto(_article.Title, body);
        try
        {
            Process.Start(new ProcessStartInfo(mailto) { UseShellExecute = true });
            Status.Text = truncated
                ? F("{0} {1}", T("A fost deschisă aplicația de e-mail pentru distribuirea articolului."), T("Articolul complet a fost copiat în clipboard."))
                : T("A fost deschisă aplicația de e-mail pentru distribuirea articolului.");
        }
        catch (Exception exception)
        {
            Status.Text = F("Aplicația de e-mail nu a putut fi deschisă: {0}", exception.Message);
            MessageBox.Show(this, Status.Text, T("Distribuire nereușită"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShareByWhatsApp_Click(object sender, RoutedEventArgs e)
    {
        var shareText = ArticleSharing.LimitForUri(ArticleSharing.BuildShareText(_article.Title, ArticleText.Text, _link), ArticleSharing.WhatsAppBodyLimit);
        var address = ArticleSharing.CreateWhatsApp(shareText);
        try
        {
            Process.Start(new ProcessStartInfo(address) { UseShellExecute = true });
            Status.Text = T("WhatsApp a fost deschis pentru distribuirea articolului.");
        }
        catch (Exception exception)
        {
            Status.Text = F("WhatsApp nu a putut fi deschis: {0}", exception.Message);
            MessageBox.Show(this, Status.Text, T("Distribuire nereușită"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
