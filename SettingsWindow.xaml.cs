using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Diagnostics;
using System.Net.Http;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class SettingsWindow : Window
{
    public enum SettingsSection
    {
        Application,
        Voice,
        Ai
    }

    private readonly SpeechService _speech = new();
    public AppSettings Settings { get; }
    public bool CleanupRequested { get; private set; }
    public bool LanguageChanged { get; private set; }
    private readonly string _initialLanguage;
    private bool _initializingSpeech = true;
    public SettingsWindow(AppSettings settings, SettingsSection section = SettingsSection.Application)
    {
        InitializeComponent();
        Settings = settings;
        SettingsIntro.Text = T("Modificările sunt păstrate numai după alegerea butonului Salvează.");
        SpeechSettingsGroup.Header = T("Citire vocală");
        SpeechEngineLabel.Content = T("Motor vocal");
        Sapi5EngineItem.Content = T("SAPI5, vocile instalate în Windows");
        EspeakEngineItem.Content = T("eSpeak NG, inclus în Orizont RSS");
        GeminiEngineItem.Content = T("Gemini TTS, voci online");
        GeminiSpeechNotice.Text = T("Gemini TTS este online. Textul citit este trimis la Google și poate consuma cota sau creditele API.");
        DeepLTitle.Text = "DeepL";
        DeepLEnabled.Content = T("Activează traducerea cu DeepL");
        DeepLNotice.Text = T("DeepL este un serviciu online. Articolul este trimis la DeepL și consumă limita contului API.");
        DeepLInstructions.Text = T("Configurare DeepL: apasă Obține cheie API DeepL, creează contul API Free, deschide API Keys & Limits, creează și copiază cheia aici, apoi testează și salvează.");
        TestDeepLButton.Content = T("Testează conexiunea DeepL");
        SoundAlerts.Content = T("Activează alertele sonore");
        AutomationProperties.SetName(SoundAlerts, T("Activează alertele sonore"));
        AutomationProperties.SetHelpText(SoundAlerts, T("Redă un sunet la finalizarea actualizării și un sunet de avertizare când există erori de feed. Nu întrerupe citirea vocală."));
        SoundAlertOnSuccess.Content = T("Alertă la finalizarea reușită");
        AutomationProperties.SetName(SoundAlertOnSuccess, T("Alertă la finalizarea reușită"));
        SoundAlertOnNewArticles.Content = T("Alertă când există articole noi");
        AutomationProperties.SetName(SoundAlertOnNewArticles, T("Alertă când există articole noi"));
        SoundAlertOnErrors.Content = T("Alertă la erori de feed");
        AutomationProperties.SetName(SoundAlertOnErrors, T("Alertă la erori de feed"));
        TestSoundButton.Content = T("Testează sunetul");
        AutomationProperties.SetName(TestSoundButton, T("Testează sunetul"));
        EspeakPitchLabel.Content = T("Înălțimea vocii eSpeak, de la 0 la 100");
        AutomationProperties.SetName(SpeechEngine, T("Motor vocal"));
        AutomationProperties.SetName(SpeechVoice, T("Vocea motorului vocal"));
        AutomationProperties.SetName(EspeakPitch, T("Înălțimea vocii eSpeak"));
        _initialLanguage = UiCulture.NormalizeSelection(settings.UiLanguage);
        foreach (var language in UiCulture.SupportedLanguages) UiLanguage.Items.Add(language);
        UiLanguage.SelectedItem = UiLanguage.Items.Cast<UiLanguage>().First(language => language.Code == _initialLanguage);
        AutoCleanup.IsChecked = settings.AutoCleanupEnabled;
        UpdateAtStartup.IsChecked = settings.UpdateAtStartup;
        SoundAlerts.IsChecked = settings.SoundAlertsEnabled;
        SoundAlertOnSuccess.IsChecked = settings.SoundAlertOnSuccess;
        SoundAlertOnNewArticles.IsChecked = settings.SoundAlertOnNewArticles;
        SoundAlertOnErrors.IsChecked = settings.SoundAlertOnErrors;
        HideRepeatedArticles.IsChecked = settings.HideRepeatedArticlesInGlobalViews;
        foreach (ComboBoxItem item in ReadNowFavoriteDays.Items) if (item.Tag?.ToString() == settings.ReadNowFavoriteDays.ToString()) { ReadNowFavoriteDays.SelectedItem = item; break; }
        if (ReadNowFavoriteDays.SelectedIndex < 0) ReadNowFavoriteDays.SelectedIndex = 2;
        StopSpeechWhenLeavingArticle.IsChecked = settings.StopSpeechWhenLeavingArticle;
        for (var rate = -10; rate <= 10; rate++) SpeechRate.Items.Add(rate);
        SpeechRate.SelectedItem = Math.Clamp(settings.SpeechRate, -10, 10);
        for (var pitch = 0; pitch <= 100; pitch += 10) EspeakPitch.Items.Add(pitch);
        EspeakPitch.SelectedItem = Math.Clamp((settings.EspeakPitch / 10) * 10, 0, 100);
        SpeechEngine.SelectedItem = SpeechEngine.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), SpeechEngineIds.Normalize(settings.SpeechEngine), StringComparison.OrdinalIgnoreCase))
            ?? SpeechEngine.Items[0];
        _initializingSpeech = false;
        PopulateSpeechVoices();
        foreach (ComboBoxItem item in SpeechVolume.Items) if (item.Tag?.ToString() == Math.Clamp(settings.SpeechVolume, 0, 100).ToString()) { SpeechVolume.SelectedItem = item; break; }
        if (SpeechVolume.SelectedIndex < 0) SpeechVolume.SelectedIndex = 3;
        AiInstructions.Text = settings.AiInstructions;
        GeminiEnabled.IsChecked = settings.GeminiEnabled;
        DeepLEnabled.IsChecked = settings.DeepLEnabled;
        try { DeepLKey.Password = SecretProtector.Unprotect(settings.EncryptedDeepLKey); }
        catch { DeepLStatus.Text = T("Cheia DeepL salvată nu poate fi citită pentru acest cont Windows."); }
        try { GeminiKey.Password = SecretProtector.Unprotect(settings.EncryptedGeminiKey); }
        catch { GeminiStatus.Text = T("Cheia Gemini salvată nu poate fi citită pentru acest cont Windows."); }
        foreach (ComboBoxItem item in RetentionDays.Items) if (item.Tag?.ToString() == settings.RetentionDays.ToString()) { RetentionDays.SelectedItem = item; break; }
        if (RetentionDays.SelectedIndex < 0) RetentionDays.SelectedIndex = 2;
        if (section != SettingsSection.Application)
        {
            LanguageGroup.Visibility = Visibility.Collapsed;
            StorageGroup.Visibility = Visibility.Collapsed;
            UpdateGroup.Visibility = Visibility.Collapsed;
            CleanupButton.Visibility = Visibility.Collapsed;
            if (section == SettingsSection.Voice)
            {
                Title = SettingsTitle.Text = T("Setări voce");
                AiSettingsGroup.Visibility = Visibility.Collapsed;
                Loaded += (_, _) => SpeechEngine.Focus();
            }
            else
            {
                Title = SettingsTitle.Text = T("Setări Inteligență artificială");
                SpeechSettingsGroup.Visibility = Visibility.Collapsed;
                Loaded += (_, _) => GeminiEnabled.Focus();
            }
        }
        else
        {
            Title = SettingsTitle.Text = T("Setări aplicație");
            SpeechSettingsGroup.Visibility = Visibility.Collapsed;
            AiSettingsGroup.Visibility = Visibility.Collapsed;
            Loaded += (_, _) => UiLanguage.Focus();
        }
        Closed += (_, _) => _speech.Dispose();
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Settings.AutoCleanupEnabled = AutoCleanup.IsChecked == true;
        SaveLanguageSetting();
        Settings.UpdateAtStartup = UpdateAtStartup.IsChecked == true;
        Settings.SoundAlertsEnabled = SoundAlerts.IsChecked == true;
        Settings.SoundAlertOnSuccess = SoundAlertOnSuccess.IsChecked == true;
        Settings.SoundAlertOnNewArticles = SoundAlertOnNewArticles.IsChecked == true;
        Settings.SoundAlertOnErrors = SoundAlertOnErrors.IsChecked == true;
        Settings.HideRepeatedArticlesInGlobalViews = HideRepeatedArticles.IsChecked == true;
        Settings.ReadNowFavoriteDays = int.TryParse((ReadNowFavoriteDays.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out var readNowDays) ? readNowDays : 7;
        SaveSpeechSettings();
        Settings.RetentionDays = int.TryParse((RetentionDays.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out var days) ? days : 90;
        Settings.AiInstructions = AiInstructions.Text.Trim();
        Settings.GeminiEnabled = GeminiEnabled.IsChecked == true;
        Settings.EncryptedGeminiKey = string.IsNullOrWhiteSpace(GeminiKey.Password) ? null : SecretProtector.Protect(GeminiKey.Password);
        SaveDeepLSettings();
        DialogResult = true;
    }
    private void SaveLanguageSetting()
    {
        var selected = UiLanguage.SelectedItem as UiLanguage;
        Settings.UiLanguage = UiCulture.NormalizeSelection(selected?.Code);
        LanguageChanged = !string.Equals(_initialLanguage, Settings.UiLanguage, StringComparison.OrdinalIgnoreCase);
    }
    private void SaveSpeechSettings()
    {
        Settings.SpeechEngine = SelectedSpeechEngine();
        var voice = SpeechVoice.IsEnabled ? SpeechVoice.SelectedItem as SpeechVoiceChoice : null;
        if (Settings.SpeechEngine == SpeechEngineIds.EspeakNg) Settings.EspeakVoiceName = voice?.Id ?? "ro";
        else if (Settings.SpeechEngine == SpeechEngineIds.GeminiTts) Settings.GeminiVoiceName = voice?.Id ?? "Charon";
        else Settings.SpeechVoiceName = voice?.Id;
        Settings.EspeakPitch = EspeakPitch.SelectedItem is int pitch ? pitch : 50;
        Settings.SpeechRate = SpeechRate.SelectedItem is int rate ? rate : 0;
        Settings.SpeechVolume = int.TryParse((SpeechVolume.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out var volume) ? volume : 100;
        Settings.StopSpeechWhenLeavingArticle = StopSpeechWhenLeavingArticle.IsChecked == true;
    }
    private void TestSpeech_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedSpeechEngine() == SpeechEngineIds.GeminiTts && string.IsNullOrWhiteSpace(ReadSavedGeminiKey()))
        {
            SpeechStatus.Text = T("Gemini TTS necesită o cheie API Gemini salvată în setările Inteligență artificială.");
            MessageBox.Show(this, SpeechStatus.Text, T("Test citire vocală"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var voice = SpeechVoice.SelectedItem as SpeechVoiceChoice;
        var rate = SpeechRate.SelectedItem is int selectedRate ? selectedRate : 0;
        var volume = int.TryParse((SpeechVolume.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out var selectedVolume) ? selectedVolume : 100;
        var configuration = new SpeechConfiguration(
            SelectedSpeechEngine(),
            SelectedSpeechEngine() == SpeechEngineIds.Sapi5 ? voice?.Id : Settings.SpeechVoiceName,
            SelectedSpeechEngine() == SpeechEngineIds.EspeakNg ? voice?.Id : Settings.EspeakVoiceName,
            rate,
            volume,
            EspeakPitch.SelectedItem is int pitch ? pitch : 50,
            SelectedSpeechEngine() == SpeechEngineIds.GeminiTts ? voice?.Id : Settings.GeminiVoiceName,
            ReadSavedGeminiKey());
        if (!_speech.Configure(configuration) || !_speech.Speak(T("Aceasta este vocea selectată pentru Orizont RSS.")))
        {
            SpeechStatus.Text = T("Vocea nu a putut fi testată.");
            MessageBox.Show(this, SpeechStatus.Text, T("Test citire vocală"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        SpeechStatus.Text = F("Se testează vocea {0}.", voice?.DisplayName ?? T("implicită"));
    }

    private void SpeechEngine_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializingSpeech || !IsInitialized) return;
        _speech.Stop(reportState: false);
        PopulateSpeechVoices();
    }

    private string SelectedSpeechEngine() =>
        SpeechEngineIds.Normalize((SpeechEngine.SelectedItem as ComboBoxItem)?.Tag?.ToString());

    private void PopulateSpeechVoices()
    {
        var engine = SelectedSpeechEngine();
        var selectedId = engine switch
        {
            SpeechEngineIds.EspeakNg => Settings.EspeakVoiceName,
            SpeechEngineIds.GeminiTts => Settings.GeminiVoiceName,
            _ => Settings.SpeechVoiceName
        };
        SpeechVoice.Items.Clear();
        foreach (var voice in _speech.InstalledVoices(engine)) SpeechVoice.Items.Add(voice);

        SpeechVoice.IsEnabled = SpeechVoice.Items.Count > 0;
        TestSpeechButton.IsEnabled = SpeechVoice.IsEnabled;
        EspeakPitchPanel.Visibility = engine == SpeechEngineIds.EspeakNg ? Visibility.Visible : Visibility.Collapsed;
        GeminiSpeechNotice.Visibility = engine == SpeechEngineIds.GeminiTts ? Visibility.Visible : Visibility.Collapsed;
        SpeechVoiceLabel.Content = engine switch
        {
            SpeechEngineIds.EspeakNg => T("Voce eSpeak NG"),
            SpeechEngineIds.GeminiTts => T("Voce Gemini online"),
            _ => T("Voce SAPI5 instalată")
        };

        if (SpeechVoice.Items.Count > 0)
        {
            SpeechVoice.SelectedItem = SpeechVoice.Items.Cast<SpeechVoiceChoice>()
                .FirstOrDefault(item => string.Equals(item.Id, selectedId, StringComparison.OrdinalIgnoreCase))
                ?? SpeechVoice.Items[0];
            SpeechStatus.Text = engine switch
            {
                SpeechEngineIds.EspeakNg => F("Au fost găsite {0} voci eSpeak NG incluse.", SpeechVoice.Items.Count),
                SpeechEngineIds.GeminiTts when string.IsNullOrWhiteSpace(ReadSavedGeminiKey()) => F("Sunt disponibile {0} voci Gemini. Pentru test este necesară o cheie API salvată.", SpeechVoice.Items.Count),
                SpeechEngineIds.GeminiTts => F("Sunt disponibile {0} voci Gemini online.", SpeechVoice.Items.Count),
                _ => F("Au fost găsite {0} voci SAPI5 instalate.", SpeechVoice.Items.Count)
            };
            return;
        }

        SpeechVoice.Items.Add(engine == SpeechEngineIds.EspeakNg ? T("Nicio voce eSpeak NG disponibilă") : T("Nicio voce SAPI5 disponibilă"));
        SpeechVoice.SelectedIndex = 0;
        SpeechStatus.Text = engine == SpeechEngineIds.EspeakNg
            ? T("Motorul eSpeak NG inclus nu este disponibil.")
            : T("Nu a fost găsită nicio voce SAPI5 instalată.");
    }

    private string? ReadSavedGeminiKey()
    {
        try { return SecretProtector.Unprotect(Settings.EncryptedGeminiKey); }
        catch { return null; }
    }
    private void SaveDeepLSettings()
    {
        Settings.DeepLEnabled = DeepLEnabled.IsChecked == true;
        Settings.EncryptedDeepLKey = string.IsNullOrWhiteSpace(DeepLKey.Password) ? null : SecretProtector.Protect(DeepLKey.Password);
    }
    private async void TestDeepL_Click(object sender, RoutedEventArgs e)
    {
        var key = DeepLKey.Password;
        if (string.IsNullOrWhiteSpace(key))
        {
            DeepLStatus.Text = T("Introdu mai întâi cheia API DeepL.");
            MessageBox.Show(this, DeepLStatus.Text, T("Test DeepL"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DeepLStatus.Text = T("Se testează conexiunea DeepL. Nu este trimis niciun articol.");
        TestDeepLButton.IsEnabled = false;
        try
        {
            var usage = await new DeepLConnection().TestAsync(key);
            DeepLEnabled.IsChecked = true;
            DeepLStatus.Text = usage.CharacterLimit > 0
                ? F("Conexiune DeepL reușită. Utilizare: {0} din {1} caractere. Alege Salvează pentru păstrarea setării.", usage.CharacterCount, usage.CharacterLimit)
                : T("Conexiune DeepL reușită. Alege Salvează pentru păstrarea setării.");
            MessageBox.Show(this, DeepLStatus.Text, T("Test DeepL"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            DeepLStatus.Text = F("Conexiunea DeepL a eșuat: {0}", exception.Message);
            MessageBox.Show(this, DeepLStatus.Text, T("Test DeepL"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { TestDeepLButton.IsEnabled = true; }
    }
    private void StopSpeechTest_Click(object sender, RoutedEventArgs e)
    {
        _speech.Stop(reportState: false);
        SpeechStatus.Text = T("Testul vocal a fost oprit.");
    }
    private void TestSound_Click(object sender, RoutedEventArgs e)
    {
        SoundAlertService.Test();
        SoundAlertStatus.Text = T("Sunetul de test a fost redat.");
    }
    private void GeminiKey_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("https://aistudio.google.com/app/apikey") { UseShellExecute = true }); }
        catch (Exception exception) { MessageBox.Show(this, F("Pagina Google AI Studio nu a putut fi deschisă.\n\n{0}", exception.Message), T("Deschidere nereușită"), MessageBoxButton.OK, MessageBoxImage.Error); }
    }
    private void DeepLKey_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("https://www.deepl.com/en/developers") { UseShellExecute = true }); }
    catch (Exception exception) { MessageBox.Show(this, F("Pagina DeepL pentru cheia API nu a putut fi deschisă. {0}", exception.Message), T("Deschidere nereușită"), MessageBoxButton.OK, MessageBoxImage.Error); }
    }
    private async void TestGemini_Click(object sender, RoutedEventArgs e)
    {
        var key = GeminiKey.Password;
        if (string.IsNullOrWhiteSpace(key))
        {
            GeminiStatus.Text = T("Introdu mai întâi cheia API Gemini.");
            MessageBox.Show(this, GeminiStatus.Text, T("Test Gemini"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        GeminiStatus.Text = T("Se testează conexiunea Gemini. Nu este trimis niciun articol.");
        TestGeminiButton.IsEnabled = false;
        TestGeminiButton.Content = T("Test Gemini în curs...");
        await Task.Yield();
        string result;
        MessageBoxImage icon;
        try
        {
            await new GeminiConnection().TestAsync(key);
            GeminiEnabled.IsChecked = true;
            result = T("Conexiune Gemini reușită. Cheia este validă și Gemini a fost activat. Alege Salvează pentru păstrarea setării.");
            icon = MessageBoxImage.Information;
        }
        catch (HttpRequestException)
        {
            result = T("Conexiune Gemini eșuată: nu s-a putut ajunge la serviciul Google. Verifică internetul, firewall-ul sau proxy-ul.");
            icon = MessageBoxImage.Error;
        }
        catch (TaskCanceledException)
        {
            result = T("Conexiune Gemini eșuată: serverul nu a răspuns în 8 secunde.");
            icon = MessageBoxImage.Error;
        }
        catch (Exception exception) { result = F("Conexiune Gemini eșuată: {0}", exception.Message); icon = MessageBoxImage.Error; }
        finally { TestGeminiButton.IsEnabled = true; TestGeminiButton.Content = T("Testează conexiunea Gemini"); }
        GeminiStatus.Text = result;
        MessageBox.Show(this, result, T("Test Gemini"), MessageBoxButton.OK, icon);
    }
    private void CleanupNow_Click(object sender, RoutedEventArgs e)
    {
        Settings.AutoCleanupEnabled = AutoCleanup.IsChecked == true;
        SaveLanguageSetting();
        Settings.UpdateAtStartup = UpdateAtStartup.IsChecked == true;
        Settings.SoundAlertsEnabled = SoundAlerts.IsChecked == true;
        Settings.SoundAlertOnSuccess = SoundAlertOnSuccess.IsChecked == true;
        Settings.SoundAlertOnNewArticles = SoundAlertOnNewArticles.IsChecked == true;
        Settings.SoundAlertOnErrors = SoundAlertOnErrors.IsChecked == true;
        Settings.HideRepeatedArticlesInGlobalViews = HideRepeatedArticles.IsChecked == true;
        SaveSpeechSettings();
        Settings.RetentionDays = int.TryParse((RetentionDays.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out var days) ? days : 90;
        Settings.AiInstructions = AiInstructions.Text.Trim();
        Settings.GeminiEnabled = GeminiEnabled.IsChecked == true;
        Settings.EncryptedGeminiKey = string.IsNullOrWhiteSpace(GeminiKey.Password) ? null : SecretProtector.Protect(GeminiKey.Password);
        SaveDeepLSettings();
        CleanupRequested = true;
        DialogResult = true;
    }
    private static string T(string source) => UiText.Translate(source);
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);
}
