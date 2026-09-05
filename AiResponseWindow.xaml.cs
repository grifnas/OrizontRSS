using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class AiResponseWindow : Window
{
    private readonly Func<string, Task<string>> _continueConversation;
    private readonly Func<string, Task> _saveNote;
    private readonly string _articleTitle;
    private readonly string _articleLink;
    private readonly string _responseTitle;
    private readonly SpeechService? _speech;
    private readonly Action<string>? _speechStateHandler;
    private bool _sending;
    public AiResponseWindow(string title, string response, string articleTitle, string articleLink, Func<string, Task<string>> continueConversation, Func<string, Task> saveNote, SpeechService? speech)
    {
        InitializeComponent();
        Title = T(title);
        ResponseSpeechMenuItem.Header = T("Citire vocală");
        SpeakConversationButton.Content = T("Citește selecția sau conversația");
        SpeakConversationMenuItem.Header = T("Citește selecția sau conversația");
        SpeakFromCursorMenuItem.Header = T("Citește cu voce de la cursor");
        PauseSpeechMenuItem.Header = PauseSpeechButton.Content = T("Pauză sau continuă");
        StopSpeechMenuItem.Header = StopSpeechButton.Content = T("Oprește citirea");
        ResponseCopyShareMenuItem.Header = T("Copiere și distribuire");
        CopyConversationMenuItem.Header = T("Copiază conversația");
        CopyResponseDocumentButton.Content = T("Copiază conversația AI cu articolul și sursa");
        CopyConversationDocumentMenuItem.Header = T("Copiază conversația AI cu articolul și sursa");
        EmailResponseButton.Content = T("Distribuie conversația AI prin e-mail");
        EmailConversationMenuItem.Header = T("Distribuie conversația AI prin e-mail");
        WhatsAppResponseButton.Content = T("Distribuie conversația AI prin WhatsApp");
        WhatsAppConversationMenuItem.Header = T("Distribuie conversația AI prin WhatsApp");
        FocusFollowUpMenuItem.Header = T("Continuă conversația");
        Response.Text = response;
        _responseTitle = T(title);
        _articleTitle = articleTitle;
        _articleLink = articleLink;
        _continueConversation = continueConversation;
        _saveNote = saveNote;
        _speech = speech;
        if (_speech is not null)
        {
            _speechStateHandler = message => Dispatcher.Invoke(() => StatusAnnouncer.Set(SpeechStatus, message, ResponseStatusBar));
            _speech.StateChanged += _speechStateHandler;
        }
        Loaded += (_, _) => { Response.Focus(); Response.CaretIndex = 0; Response.Select(0, 0); };
        Closed += (_, _) =>
        {
            _speech?.Stop(reportState: false);
            if (_speech is not null && _speechStateHandler is not null) _speech.StateChanged -= _speechStateHandler;
        };
    }
    private void SpeakConversation_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureSpeechAvailable()) return;
        var text = Response.SelectionLength > 0 ? Response.SelectedText : Response.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show(this, T("Nu există încă un răspuns de citit."), T("Răspuns AI"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        _speech!.Speak(text);
    }
    private void SpeakFromCursor_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureSpeechAvailable()) return;
        var start = Math.Clamp(Response.CaretIndex, 0, Response.Text.Length);
        var text = Response.Text[start..];
        if (string.IsNullOrWhiteSpace(text)) text = Response.Text;
        if (!string.IsNullOrWhiteSpace(text)) _speech!.Speak(text);
    }
    private void PauseResumeSpeech_Click(object sender, RoutedEventArgs e)
    {
        if (EnsureSpeechAvailable()) _speech!.PauseOrResume();
    }
    private void StopSpeech_Click(object sender, RoutedEventArgs e) => _speech?.Stop();
    private void FocusFollowUp_Click(object sender, RoutedEventArgs e)
    {
        FollowUpQuestion.Focus();
        FollowUpQuestion.CaretIndex = FollowUpQuestion.Text.Length;
    }
    private bool EnsureSpeechAvailable()
    {
        if (_speech?.EnsureAvailable() == true) return true;
        var message = T("Motorul vocal selectat nu este disponibil. Verifică motorul și vocea în Setări.");
        StatusAnnouncer.Set(SpeechStatus, message, ResponseStatusBar);
        MessageBox.Show(this, message, T("Citire vocală indisponibilă"), MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.F11 && Keyboard.Modifiers == ModifierKeys.None)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            var message = WindowState == WindowState.Maximized
                ? T("Fereastra a fost maximizată.")
                : T("Fereastra a fost restabilită.");
            StatusAnnouncer.Set(SpeechStatus, message, ResponseStatusBar);
            e.Handled = true;
            return;
        }
        if (key == Key.F9 && Keyboard.Modifiers == ModifierKeys.None)
        {
            if (_speech?.IsSpeaking == true || _speech?.IsPaused == true) PauseResumeSpeech_Click(this, e);
            else SpeakConversation_Click(this, e);
            e.Handled = true;
            return;
        }
        if (key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None && (_speech?.IsSpeaking == true || _speech?.IsPaused == true))
        {
            _speech.Stop();
            e.Handled = true;
            return;
        }
        if (Keyboard.Modifiers != (ModifierKeys.Control | ModifierKeys.Alt)) return;
        if (key == Key.V) SpeakConversation_Click(this, e);
        else if (key == Key.P) PauseResumeSpeech_Click(this, e);
        else if (key == Key.S) StopSpeech_Click(this, e);
        else return;
        e.Handled = true;
    }
    private string ResponseDocument() => F("Articol: {0}{1}Adresă: {2}{1}Tip răspuns AI: {3}{1}Generat/exportat: {4}{1}{1}{5}", _articleTitle, Environment.NewLine, _articleLink, _responseTitle, DateTimeOffset.Now.ToString("g", CultureInfo.CurrentCulture), Response.Text.Trim());
    private void CopyResponse_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Response.Text)) { MessageBox.Show(this, T("Nu există încă un răspuns de copiat."), T("Răspuns AI"), MessageBoxButton.OK, MessageBoxImage.Information); return; }
        Clipboard.SetText(Response.Text.Trim());
        MessageBox.Show(this, T("Răspunsul AI a fost copiat în clipboard."), T("Răspuns AI"), MessageBoxButton.OK, MessageBoxImage.Information);
    }
    private void CopyResponseDocument_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Response.Text)) { MessageBox.Show(this, T("Nu există încă un răspuns de copiat."), T("Răspuns AI"), MessageBoxButton.OK, MessageBoxImage.Information); return; }
        Clipboard.SetText(ResponseDocument());
        MessageBox.Show(this, T("Conversația AI completă, cu articolul și sursa, a fost copiată în clipboard."), T("Răspuns AI"), MessageBoxButton.OK, MessageBoxImage.Information);
    }
    private async void SaveNote_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Response.Text)) { MessageBox.Show(this, T("Nu există încă un răspuns de salvat."), T("Răspuns AI"), MessageBoxButton.OK, MessageBoxImage.Information); return; }
        try
        {
            await _saveNote(Response.Text.Trim());
            MessageBox.Show(this, T("Răspunsul AI a fost salvat ca notiță a articolului. Va fi inclus și în copia de siguranță."), T("Răspuns AI"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception) { MessageBox.Show(this, F("Notița nu a putut fi salvată: {0}", exception.Message), T("Răspuns AI"), MessageBoxButton.OK, MessageBoxImage.Error); }
    }
    private void ExportResponse_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Response.Text)) { MessageBox.Show(this, T("Nu există încă un răspuns de exportat."), T("Răspuns AI"), MessageBoxButton.OK, MessageBoxImage.Information); return; }
        var dialog = new SaveFileDialog { Filter = T("Fișier text (*.txt)|*.txt|Markdown (*.md)|*.md"), FileName = $"{SafeFileName(_articleTitle)} - {_responseTitle}.txt", AddExtension = true, OverwritePrompt = true };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            File.WriteAllText(dialog.FileName, ResponseDocument(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            MessageBox.Show(this, T("Răspunsul AI a fost exportat în fișier."), T("Răspuns AI"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception) { MessageBox.Show(this, F("Fișierul nu a putut fi salvat: {0}", exception.Message), T("Răspuns AI"), MessageBoxButton.OK, MessageBoxImage.Error); }
    }
    private void EmailResponse_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Response.Text)) { MessageBox.Show(this, T("Nu există încă un răspuns de trimis."), T("Răspuns AI"), MessageBoxButton.OK, MessageBoxImage.Information); return; }
        var document = ResponseDocument();
        const int emailBodyLimit = 1800;
        var body = document.Length <= emailBodyLimit ? document : F("Răspunsul AI complet pentru articolul „{0}” a fost copiat în clipboard. Lipește-l în acest mesaj cu Ctrl+V.{1}{1}Sursă: {2}", _articleTitle, Environment.NewLine, _articleLink);
        if (document.Length > emailBodyLimit) Clipboard.SetText(document);
        var mailto = $"mailto:?subject={Uri.EscapeDataString($"{_responseTitle}: {_articleTitle}")}&body={Uri.EscapeDataString(body)}";
        try
        {
            Process.Start(new ProcessStartInfo(mailto) { UseShellExecute = true });
            if (document.Length > emailBodyLimit) MessageBox.Show(this, T("A fost deschisă aplicația de e-mail. Răspunsul complet este în clipboard; lipește-l în mesaj cu Ctrl+V."), T("Răspuns AI"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception) { MessageBox.Show(this, F("Aplicația de e-mail nu a putut fi deschisă: {0}", exception.Message), T("Răspuns AI"), MessageBoxButton.OK, MessageBoxImage.Error); }
    }
    private void WhatsAppResponse_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Response.Text)) { MessageBox.Show(this, T("Nu există încă un răspuns de trimis."), T("Răspuns AI"), MessageBoxButton.OK, MessageBoxImage.Information); return; }
        var document = ResponseDocument();
        var shareText = ArticleSharing.LimitForUri(document, ArticleSharing.WhatsAppBodyLimit);
        if (document.Length > ArticleSharing.WhatsAppBodyLimit)
        {
            Clipboard.SetText(document);
            shareText = F("Răspunsul AI complet pentru articolul „{0}” a fost copiat în clipboard. Lipește-l în WhatsApp cu Ctrl+V.", _articleTitle);
        }
        var address = ArticleSharing.CreateWhatsApp(shareText);
        try
        {
            Process.Start(new ProcessStartInfo(address) { UseShellExecute = true });
            MessageBox.Show(this, document.Length > ArticleSharing.WhatsAppBodyLimit
                ? T("WhatsApp a fost deschis. Conversația AI completă este în clipboard; lipește-o cu Ctrl+V.")
                : T("WhatsApp a fost deschis pentru distribuirea conversației AI."), T("Răspuns AI"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception) { MessageBox.Show(this, F("WhatsApp nu a putut fi deschis: {0}", exception.Message), T("Răspuns AI"), MessageBoxButton.OK, MessageBoxImage.Error); }
    }
    private static string SafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(name.Select(character => invalid.Contains(character) ? '_' : character).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(safe) ? T("Răspuns AI") : safe[..Math.Min(safe.Length, 80)];
    }
    private async void SendFollowUp_Click(object sender, RoutedEventArgs e) => await SendFollowUpAsync();
    private async void FollowUpQuestion_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.Modifiers != ModifierKeys.Control) return;
        await SendFollowUpAsync();
        e.Handled = true;
    }
    private async Task SendFollowUpAsync()
    {
        var question = FollowUpQuestion.Text.Trim();
        if (_sending || string.IsNullOrWhiteSpace(question))
        {
            if (string.IsNullOrWhiteSpace(question)) MessageBox.Show(this, T("Scrie mai întâi o întrebare."), "Gemini", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _sending = true;
        SendFollowUpButton.IsEnabled = false;
        SendFollowUpButton.Content = T("Gemini răspunde...");
        FollowUpQuestion.IsEnabled = false;
        try
        {
            var previous = Response.Text;
            Response.AppendText(F("{0}{0}Întrebarea ta: {1}{0}{0}Gemini răspunde...{0}", Environment.NewLine, question));
            Response.ScrollToEnd();
            var answer = await _continueConversation($"Conversația de până acum:\n{previous}\n\nÎntrebarea nouă a utilizatorului: {question}\nRăspunde direct la întrebarea nouă, folosind articolul și conversația.");
            RemovePendingIndicator();
            Response.AppendText(F("Răspuns Gemini: {0}", answer));
            FollowUpQuestion.Clear();
            Response.ScrollToEnd();
            Response.Focus();
        }
        catch (TaskCanceledException) { RemovePendingIndicator(); MessageBox.Show(this, T("Gemini nu a răspuns în 45 de secunde. Poți încerca întrebarea din nou."), "Gemini", MessageBoxButton.OK, MessageBoxImage.Error); }
        catch (Exception exception) { RemovePendingIndicator(); MessageBox.Show(this, F("Gemini nu a putut răspunde: {0}", exception.Message), "Gemini", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally
        {
            _sending = false;
            SendFollowUpButton.IsEnabled = true;
            SendFollowUpButton.Content = T("Trimite întrebarea");
            FollowUpQuestion.IsEnabled = true;
            FollowUpQuestion.Focus();
        }
    }
    private void RemovePendingIndicator()
    {
        var pending = T("Gemini răspunde...") + Environment.NewLine;
        if (Response.Text.EndsWith(pending, StringComparison.Ordinal)) Response.Text = Response.Text[..^pending.Length];
    }
    private static string T(string source) => UiText.Translate(source);
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);
}
