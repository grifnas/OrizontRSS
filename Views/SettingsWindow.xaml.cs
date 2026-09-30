using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Diagnostics;
using System.Net.Http;
using CititorRSS.Jaws.Localization;
using CititorRSS.Jaws.Services.Content;

namespace CititorRSS.Jaws;

public partial class SettingsWindow : Window
{
    private void ShortcutSettings_Click(object sender, RoutedEventArgs e)
        => new ShortcutSettingsWindow(Settings) { Owner = this }.ShowDialog();

    public enum SettingsSection
    {
        Application,
        Voice,
        Ai,
        Feeds,
        Reader,
        Storage,
        NewsBlur
    }

    private readonly SpeechService _speech = new();
    public AppSettings Settings { get; }
    public bool CleanupRequested { get; private set; }
    public bool LanguageChanged { get; private set; }
    private readonly string _initialLanguage;
    private bool _initializingSpeech = true;
    private bool _initializingAiProvider;
    private string _pendingAiDefaultProvider = AiProviderIds.Gemini;
    public SettingsWindow(AppSettings settings, SettingsSection section = SettingsSection.Application)
    {
        InitializeComponent();
        Settings = settings;
        ShortcutSettingsButton.Visibility = section == SettingsSection.Application ? Visibility.Visible : Visibility.Collapsed;
        SettingsIntro.Text = T("Modificările sunt păstrate numai după alegerea butonului Salvează.");

        // Categorii
        CategoriesLabel.Content = T("Categorii");
        AutomationProperties.SetName(CategoryList, T("Categorii setări"));
        CategoryGeneralItem.Content = T("General");
        CategoryFeedsItem.Content = T("Feeduri și actualizare");
        CategoryReaderItem.Content = T("Cititor Orizont");
        CategoryStorageItem.Content = T("Date și stocare");
        CategoryNewsBlurItem.Content = T("Sincronizare NewsBlur");
        CategoryVoiceItem.Content = T("Citire vocală");
        CategoryAiItem.Content = T("Inteligență artificială și traducere");

        // 1. General
        AppearanceGroup.Header = T("Aspect și contrast");
        ColorSchemeLabel.Content = T("Temă de culori");
        ColorSchemeNotice.Text = T("Windows automat respectă tema Contrast aleasă în Windows. Celelalte variante sunt optimizate pentru contrast ridicat și se aplică după salvare.");
        AutomationProperties.SetName(ColorScheme, T("Temă de culori"));
        foreach (ComboBoxItem item in ColorScheme.Items)
            item.Content = T(item.Tag?.ToString() switch
            {
                ColorThemeManager.BlackOnWhite => "Negru pe alb",
                ColorThemeManager.WhiteOnBlack => "Alb pe negru",
                ColorThemeManager.BlackOnYellow => "Negru pe galben",
                ColorThemeManager.YellowOnNavy => "Galben pe albastru închis",
                _ => "Windows automat"
            });

        LanguageGroup.Header = T("Limbă");
        LanguageLabel.Content = T("Limba interfeței");
        LanguageNotice.Text = T("Limba aleasă se aplică după repornirea aplicației.");
        GeneralAppGroup.Header = T("Aplicație");
        CheckAppUpdatesAtStartup.Content = T("Verifică actualizările aplicației la pornire");
        AutomationProperties.SetName(CheckAppUpdatesAtStartup, T("Verifică actualizările aplicației la pornire"));
        ArticleExportGroup.Header = T("Export articole");
        ArticleExportFolderLabel.Content = T("Folder pentru exportul articolelor");
        AutomationProperties.SetName(ArticleExportFolder, T("Folder pentru exportul articolelor"));
        ArticleExportFolderNotice.Text = T("Folderul implicit este Documente\\Orizont RSS\\Articole. Poți alege un alt folder.");
        ArticleExportFormatLabel.Content = T("Format implicit pentru export");
        AutomationProperties.SetName(ArticleExportFormat, T("Format implicit pentru export"));
        foreach (ComboBoxItem item in ArticleExportFormat.Items)
            item.Content = T(item.Tag?.ToString() == "rtf" ? "Fișier RTF (.rtf)" : "Fișier TXT (.txt)");

        // 2. Feeduri și actualizare
        UpdateGroup.Header = T("Actualizare și afișare");
        UpdateAtStartup.Content = T("Actualizează feedurile la pornire");
        AutomationProperties.SetName(UpdateAtStartup, T("Actualizează feedurile la pornire"));
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
        HideRepeatedArticles.Content = T("Ascunde articolele repetate în vederile globale");
        AutomationProperties.SetName(HideRepeatedArticles, T("Ascunde articolele repetate în vederile globale"));
        AutomationProperties.SetHelpText(HideRepeatedArticles, T("Nu șterge articolele din feeduri. Afișează o singură copie când sunt combinate mai multe surse."));

        // 3. Cititor Orizont
        ReaderGroup.Header = T("Cititor Orizont");
        ReaderModeLabel.Content = T("Mod implicit de afișare a articolelor");
        AutomationProperties.SetName(ReaderMode, T("Mod implicit de afișare a articolelor"));
        ReaderModeTextItem.Content = T("Text simplu accesibil (cursor text clasic)");
        ReaderModeWebViewItem.Content = T("WebReader (formatat web, navigare Virtual Cursor JAWS/NVDA)");
        ReaderModeNotice.Text = T("Modul text simplu este recomandat pentru navigare tradițională cu săgețile. Modul WebReader permite navigarea prin taste rapide de browser (H pentru titluri, P pentru paragrafe, Tab pentru linkuri). Modul se poate comuta oricând și din fereastra Cititor cu combinația Ctrl+Shift+F8.");
        ArticleOpenModeLabel.Content = T("Deschiderea implicită a articolelor");
        AutomationProperties.SetName(ArticleOpenMode, T("Deschiderea implicită a articolelor"));
        ArticleOpenMode.Items[0] = new ComboBoxItem { Content = T("Mod standard în fereastra principală"), Tag = ArticleOpenModeIds.Standard };
        ArticleOpenMode.Items[1] = new ComboBoxItem { Content = T("Cititor Orizont"), Tag = ArticleOpenModeIds.Orizont };
        ArticleOpenModeNotice.Text = T("Se aplică feedurilor care folosesc opțiunea Folosește setarea generală.");
        ReaderPreferencesGroup.Header = T("Preferințe articole");
        ReadNowFavoriteDaysLabel.Content = T("Afișează articolele noi în Citește acum pentru");
        AutomationProperties.SetName(ReadNowFavoriteDays, T("Perioada articolelor noi în Citește acum"));
        ReadNowFavoriteDaysNotice.Text = T("Favoritele și articolele De citit mai târziu rămân în Citește acum indiferent de vechime, până când le elimini manual.");

        // 4. Date și stocare
        StorageGroup.Header = T("Date și stocare");
        AutoCleanup.Content = T("Șterge automat articolele obișnuite expirate");
        AutomationProperties.SetName(AutoCleanup, T("Șterge automat articolele obișnuite expirate"));
        RetentionDaysLabel.Content = T("Păstrează articolele obișnuite timp de");
        AutomationProperties.SetName(RetentionDays, T("Perioada de păstrare a articolelor obișnuite"));
        StorageNotice.Text = T("Favoritele și articolele De citit mai târziu sunt protejate și nu sunt șterse automat.");
        CleanupButton.Content = T("Curăță acum articolele expirate");

        // 5. Sincronizare NewsBlur
        NewsBlurSyncGroup.Header = T("Sincronizare NewsBlur");
        NewsBlurSavedStoryModeLabel.Content = T("Articolele salvate în NewsBlur se sincronizează ca");
        NewsBlurSavedStoryModeNotice.Text = T("Stelele NewsBlur sunt articole salvate. Alegerea se aplică la următoarea sincronizare și nu șterge articole locale.");
        AutomationProperties.SetName(NewsBlurSavedStoryMode, T("Maparea articolelor salvate NewsBlur"));
        NewsBlurSyncFeedsAndFoldersAtStartup.Content = T("Sincronizează feedurile și folderele NewsBlur la pornire");
        AutomationProperties.SetName(NewsBlurSyncFeedsAndFoldersAtStartup, T("Sincronizează feedurile și folderele NewsBlur la pornire"));
        NewsBlurSyncArticlesAndStatesAtStartup.Content = T("Sincronizează articolele și stările NewsBlur la pornire");
        AutomationProperties.SetName(NewsBlurSyncArticlesAndStatesAtStartup, T("Sincronizează articolele și stările NewsBlur la pornire"));
        NewsBlurAutoSyncEnabled.Content = T("Activează sincronizarea automată NewsBlur");
        AutomationProperties.SetName(NewsBlurAutoSyncEnabled, T("Activează sincronizarea automată NewsBlur"));
        NewsBlurAutoSyncMinutesLabel.Content = T("Interval sincronizare automată");
        AutomationProperties.SetName(NewsBlurAutoSyncMinutes, T("Interval sincronizare automată NewsBlur"));
        NewsBlurAutoSyncNotice.Text = T("Sincronizarea periodică include feeduri, foldere, articole și stări. Primele două bife controlează separat ce se sincronizează la pornire; comenzile manuale rămân disponibile.");

        // 6. Citire vocală
        SpeechSettingsGroup.Header = T("Citire vocală");
        SpeechNotice.Text = T("Vocea pornește numai la comanda ta și nu înlocuiește cititorul de ecran pentru citirea interfeței.");
        SpeechEngineLabel.Content = T("Motor vocal");
        Sapi5EngineItem.Content = T("SAPI5, vocile instalate în Windows");
        EspeakEngineItem.Content = T("eSpeak NG, inclus în Orizont RSS");
        GeminiEngineItem.Content = T("Gemini TTS, voci online");
        GeminiSpeechNotice.Text = T("Gemini TTS este online. Textul citit este trimis la Google și poate consuma cota sau creditele API.");
        EspeakPitchLabel.Content = T("Înălțimea vocii eSpeak, de la 0 la 100");
        AutomationProperties.SetName(SpeechEngine, T("Motor vocal"));
        AutomationProperties.SetName(SpeechVoice, T("Vocea motorului vocal"));
        AutomationProperties.SetName(EspeakPitch, T("Înălțimea vocii eSpeak"));
        EspeakVariantLabel.Content = T("Variantă vocală eSpeak NG");
        AutomationProperties.SetName(EspeakVariant, T("Variantă vocală eSpeak NG"));
        EspeakInflectionLabel.Content = T("Intonația vocii eSpeak, de la 0 la 100");
        AutomationProperties.SetName(EspeakInflection, T("Intonația vocii eSpeak"));
        SpeechVolumeLabel.Content = T("Volum");
        AutomationProperties.SetName(SpeechVolume, T("Volumul vocii"));
        StopSpeechWhenLeavingArticle.Content = T("Oprește vocea când ies din conținutul articolului cu Escape");
        AutomationProperties.SetName(StopSpeechWhenLeavingArticle, T("Oprește vocea când ies din conținutul articolului"));
        TestSpeechButton.Content = T("Testează vocea");
        StopSpeechTestButton.Content = T("Oprește testul");

        // 7. Inteligență artificială și traducere
        AiSettingsGroup.Header = T("Inteligență artificială și traducere");
        AiProviderLabel.Content = T("Furnizor AI");
        AutomationProperties.SetName(AiProviderSelector, T("Furnizor AI"));
        AiDefaultProvider.Content = T("Folosește ca furnizor implicit");
        AutomationProperties.SetName(AiDefaultProvider, T("Folosește ca furnizor implicit"));
        GeminiSectionTitle.Text = "Gemini";
        GeminiEnabled.Content = T("Activează Gemini");
        AutomationProperties.SetName(GeminiEnabled, T("Activează Gemini"));
        GeminiKeyLabel.Content = T("Cheie API Gemini");
        AutomationProperties.SetName(GeminiKey, T("Cheie API Gemini"));
        TestGeminiButton.Content = T("Testează conexiunea Gemini");
        GetGeminiKeyButton.Content = T("Obține cheie API Gemini");
        GeminiTestNotice.Text = T("Testul verifică modelele disponibile și nu trimite niciun articol.");
        OpenAiSectionTitle.Text = "OpenAI";
        OpenAiEnabled.Content = T("Activează OpenAI");
        AutomationProperties.SetName(OpenAiEnabled, T("Activează OpenAI"));
        OpenAiKeyLabel.Content = T("Cheie API OpenAI");
        AutomationProperties.SetName(OpenAiKey, T("Cheie API OpenAI"));
        OpenAiModelLabel.Content = T("Model OpenAI");
        AutomationProperties.SetName(OpenAiModel, T("Model OpenAI"));
        TestOpenAiButton.Content = T("Testează conexiunea OpenAI");
        GetOpenAiKeyButton.Content = T("Obține cheia API OpenAI");
        OpenAiNotice.Text = T("OpenAI API este facturat separat de abonamentul ChatGPT. Testul verifică modelele fără să trimită articol; întrebările AI trimit textul articolului către OpenAI.");
        MistralSectionTitle.Text = T("Mistral");
        MistralEnabled.Content = T("Activează Mistral");
        AutomationProperties.SetName(MistralEnabled, T("Activează Mistral"));
        MistralKeyLabel.Content = T("Cheie API Mistral");
        AutomationProperties.SetName(MistralKey, T("Cheie API Mistral"));
        MistralModelLabel.Content = T("Model Mistral");
        AutomationProperties.SetName(MistralModel, T("Model Mistral"));
        TestMistralButton.Content = T("Testează conexiunea Mistral");
        GetMistralKeyButton.Content = T("Obține cheia API Mistral");
        MistralNotice.Text = T("Mistral este un serviciu online. Testul enumeră modelele de conversație fără să trimită articolul. Când întrebi Mistral despre un articol, textul articolului este trimis furnizorului și poate consuma cota sau genera costuri în cont.");
        DeepSeekProviderItem.Content = AiProviderIds.DeepSeek;
        DeepSeekSectionTitle.Text = T("Mistral").Replace("Mistral", AiProviderIds.DeepSeek, StringComparison.Ordinal);
        DeepSeekEnabled.Content = T("Activează Mistral").Replace("Mistral", AiProviderIds.DeepSeek, StringComparison.Ordinal);
        AutomationProperties.SetName(DeepSeekEnabled, T("Activează Mistral").Replace("Mistral", AiProviderIds.DeepSeek, StringComparison.Ordinal));
        DeepSeekKeyLabel.Content = T("Cheie API Mistral").Replace("Mistral", AiProviderIds.DeepSeek, StringComparison.Ordinal);
        AutomationProperties.SetName(DeepSeekKey, T("Cheie API Mistral").Replace("Mistral", AiProviderIds.DeepSeek, StringComparison.Ordinal));
        DeepSeekModelLabel.Content = T("Model Mistral").Replace("Mistral", AiProviderIds.DeepSeek, StringComparison.Ordinal);
        AutomationProperties.SetName(DeepSeekModel, T("Model Mistral").Replace("Mistral", AiProviderIds.DeepSeek, StringComparison.Ordinal));
        TestDeepSeekButton.Content = T("Testează conexiunea Mistral").Replace("Mistral", AiProviderIds.DeepSeek, StringComparison.Ordinal);
        GetDeepSeekKeyButton.Content = T("Obține cheia API Mistral").Replace("Mistral", AiProviderIds.DeepSeek, StringComparison.Ordinal);
        DeepSeekNotice.Text = T("Mistral este un serviciu online. Testul enumeră modelele de conversație fără să trimită articolul. Când întrebi Mistral despre un articol, textul articolului este trimis furnizorului și poate consuma cota sau genera costuri în cont.").Replace("Mistral", AiProviderIds.DeepSeek, StringComparison.Ordinal);
        DeepLTitle.Text = "DeepL";
        DeepLEnabled.Content = T("Activează traducerea cu DeepL");
        AutomationProperties.SetName(DeepLEnabled, T("Activează traducerea cu DeepL"));
        DeepLKeyLabel.Content = T("Cheie API DeepL");
        AutomationProperties.SetName(DeepLKey, T("Cheie API DeepL"));
        TestDeepLButton.Content = T("Testează conexiunea DeepL");
        GetDeepLKeyButton.Content = T("Obține cheie API DeepL");
        DeepLNotice.Text = T("DeepL este un serviciu online. Articolul este trimis la DeepL și consumă limita contului API.");
        DeepLInstructions.Text = T("Configurare DeepL: apasă Obține cheie API DeepL, creează contul API Free, deschide API Keys & Limits, creează și copiază cheia aici, apoi testează și salvează.");
        GoogleTranslateTitle.Text = T("Google Translate (fără cheie API)");
        GoogleTranslateSourceLabel.Content = T("Limba sursă pentru Google Translate");
        GoogleTranslateTargetLabel.Content = T("Limba țintă pentru Google Translate");
        GoogleTranslateNotice.Text = T("Traducerea folosește un serviciu online Google. Textul complet se trimite numai după confirmarea din articol.");
        AutomationProperties.SetName(GoogleTranslateSourceLanguage, T("Limba sursă pentru Google Translate"));
        AutomationProperties.SetName(GoogleTranslateTargetLanguage, T("Limba țintă pentru Google Translate"));
        AiInstructionsLabel.Content = T("Instrucțiuni permanente pentru agent");
        AutomationProperties.SetName(AiInstructions, T("Instrucțiuni permanente pentru agentul AI"));
        AiInstructionsNotice.Text = T("Instrucțiunile se aplică rezumării, traducerii și conversațiilor viitoare despre articole.");

        // Butoane
        CancelButton.Content = T("Anulează");
        SaveButton.Content = T("Salvează");

        _initialLanguage = UiCulture.NormalizeSelection(settings.UiLanguage);
        foreach (var language in UiCulture.SupportedLanguages) UiLanguage.Items.Add(language);
        UiLanguage.SelectedItem = UiLanguage.Items.Cast<UiLanguage>().First(language => language.Code == _initialLanguage);
        ColorScheme.SelectedItem = ColorScheme.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), ColorThemeManager.Normalize(settings.ColorScheme), StringComparison.OrdinalIgnoreCase))
            ?? ColorScheme.Items[0];
        ReaderMode.SelectedItem = ReaderMode.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), ReaderModeIds.Normalize(settings.ReaderMode), StringComparison.OrdinalIgnoreCase))
            ?? ReaderMode.Items[0];
        ArticleOpenMode.SelectedItem = ArticleOpenMode.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), ArticleOpenModeIds.Normalize(settings.ArticleOpenMode), StringComparison.OrdinalIgnoreCase))
            ?? ArticleOpenMode.Items[0];
        AutoCleanup.IsChecked = settings.AutoCleanupEnabled;
        UpdateAtStartup.IsChecked = settings.UpdateAtStartup;
        CheckAppUpdatesAtStartup.IsChecked = settings.CheckAppUpdatesAtStartup;
        ArticleExportFolder.Text = settings.ArticleExportFolder;
        ArticleExportFormat.SelectedItem = ArticleExportFormat.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), settings.ArticleExportFormat, StringComparison.OrdinalIgnoreCase))
            ?? ArticleExportFormat.Items[0];
        SoundAlerts.IsChecked = settings.SoundAlertsEnabled;
        SoundAlertOnSuccess.IsChecked = settings.SoundAlertOnSuccess;
        SoundAlertOnNewArticles.IsChecked = settings.SoundAlertOnNewArticles;
        SoundAlertOnErrors.IsChecked = settings.SoundAlertOnErrors;
        HideRepeatedArticles.IsChecked = settings.HideRepeatedArticlesInGlobalViews;
        NewsBlurSavedStoryMode.SelectedItem = NewsBlurSavedStoryMode.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), NormalizeNewsBlurSavedStoryMode(settings.NewsBlurSavedStoryMode), StringComparison.OrdinalIgnoreCase))
            ?? NewsBlurSavedStoryMode.Items[0];
        NewsBlurSyncFeedsAndFoldersAtStartup.IsChecked = settings.NewsBlurSyncFeedsAndFoldersAtStartup;
        NewsBlurSyncArticlesAndStatesAtStartup.IsChecked = settings.NewsBlurSyncArticlesAndStatesAtStartup;
        NewsBlurAutoSyncEnabled.IsChecked = settings.NewsBlurAutoSyncEnabled;
        NewsBlurAutoSyncMinutes.SelectedItem = NewsBlurAutoSyncMinutes.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => int.TryParse(item.Tag?.ToString(), out var minutes) && minutes == NormalizeNewsBlurAutoSyncMinutes(settings.NewsBlurAutoSyncMinutes))
            ?? NewsBlurAutoSyncMinutes.Items[1];
        foreach (ComboBoxItem item in ReadNowFavoriteDays.Items) if (item.Tag?.ToString() == settings.ReadNowFavoriteDays.ToString()) { ReadNowFavoriteDays.SelectedItem = item; break; }
        if (ReadNowFavoriteDays.SelectedIndex < 0) ReadNowFavoriteDays.SelectedIndex = 2;
        StopSpeechWhenLeavingArticle.IsChecked = settings.StopSpeechWhenLeavingArticle;
        for (var rate = -10; rate <= 10; rate++) SpeechRate.Items.Add(rate);
        SpeechRate.SelectedItem = Math.Clamp(settings.SpeechRate, -10, 10);
        for (var pitch = 0; pitch <= 100; pitch += 10) EspeakPitch.Items.Add(pitch);
        EspeakPitch.SelectedItem = Math.Clamp((settings.EspeakPitch / 10) * 10, 0, 100);
        for (var inflection = 0; inflection <= 100; inflection += 10) EspeakInflection.Items.Add(inflection);
        EspeakInflection.SelectedItem = Math.Clamp((settings.EspeakInflection / 10) * 10, 0, 100);
        SpeechEngine.SelectedItem = SpeechEngine.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), SpeechEngineIds.Normalize(settings.SpeechEngine), StringComparison.OrdinalIgnoreCase))
            ?? SpeechEngine.Items[0];
        _initializingSpeech = false;
        PopulateSpeechVoices();
        foreach (ComboBoxItem item in SpeechVolume.Items) if (item.Tag?.ToString() == Math.Clamp(settings.SpeechVolume, 0, 100).ToString()) { SpeechVolume.SelectedItem = item; break; }
        if (SpeechVolume.SelectedIndex < 0) SpeechVolume.SelectedIndex = 3;
        AiInstructions.Text = AiInstructionsLocalization.ForDisplay(settings.AiInstructions);
        _initializingAiProvider = true;
        _pendingAiDefaultProvider = AiProviderIds.Normalize(settings.AiDefaultProvider);
        AiProviderSelector.SelectedItem = AiProviderSelector.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), _pendingAiDefaultProvider, StringComparison.OrdinalIgnoreCase))
            ?? AiProviderSelector.Items[0];
        GeminiEnabled.IsChecked = settings.GeminiEnabled;
        OpenAiEnabled.IsChecked = settings.OpenAiEnabled;
        OpenAiModel.Text = string.IsNullOrWhiteSpace(settings.OpenAiModel) ? "gpt-6-luna" : settings.OpenAiModel.Trim();
        MistralEnabled.IsChecked = settings.MistralEnabled;
        MistralModel.Text = string.IsNullOrWhiteSpace(settings.MistralModel) ? "mistral-small-latest" : settings.MistralModel.Trim();
        DeepSeekEnabled.IsChecked = settings.DeepSeekEnabled;
        DeepSeekModel.Text = string.IsNullOrWhiteSpace(settings.DeepSeekModel) ? "deepseek-flash" : settings.DeepSeekModel.Trim();
        DeepLEnabled.IsChecked = settings.DeepLEnabled;
        PopulateGoogleTranslateLanguages(settings);
        try { DeepLKey.Password = SecretProtector.Unprotect(settings.EncryptedDeepLKey); }
        catch { DeepLStatus.Text = T("Cheia DeepL salvată nu poate fi citită pentru acest cont Windows."); }
        try { GeminiKey.Password = SecretProtector.Unprotect(settings.EncryptedGeminiKey); }
        catch { GeminiStatus.Text = T("Cheia Gemini salvată nu poate fi citită pentru acest cont Windows."); }
        try { OpenAiKey.Password = SecretProtector.Unprotect(settings.EncryptedOpenAiKey); }
        catch { OpenAiStatus.Text = T("Cheia OpenAI salvată nu poate fi citită pentru acest cont Windows. Introdu cheia din nou în Setări Inteligență artificială."); }
        try { MistralKey.Password = SecretProtector.Unprotect(settings.EncryptedMistralKey); }
        catch { MistralStatus.Text = F("Cheia API pentru {0} nu poate fi citită pentru acest cont Windows. Introdu cheia din nou în Setări Inteligență artificială.", AiProviderIds.Mistral); }
        try { DeepSeekKey.Password = SecretProtector.Unprotect(settings.EncryptedDeepSeekKey); }
        catch { DeepSeekStatus.Text = F("Cheia API pentru {0} nu poate fi citită pentru acest cont Windows. Introdu cheia din nou în Setări Inteligență artificială.", AiProviderIds.DeepSeek); }
        AiDefaultProvider.IsChecked = true;
        _initializingAiProvider = false;
        UpdateAiProviderPanel();
        foreach (ComboBoxItem item in RetentionDays.Items) if (item.Tag?.ToString() == settings.RetentionDays.ToString()) { RetentionDays.SelectedItem = item; break; }
        if (RetentionDays.SelectedIndex < 0) RetentionDays.SelectedIndex = 2;

        switch (section)
        {
            case SettingsSection.Voice:
                Title = SettingsTitle.Text = T("Setări voce");
                CategoryList.SelectedItem = CategoryVoiceItem;
                Loaded += (_, _) => SpeechEngine.Focus();
                break;
            case SettingsSection.Ai:
                Title = SettingsTitle.Text = T("Setări Inteligență artificială");
                CategoryList.SelectedItem = CategoryAiItem;
                Loaded += (_, _) => AiProviderSelector.Focus();
                break;
            case SettingsSection.Feeds:
                Title = SettingsTitle.Text = T("Setări aplicație");
                CategoryList.SelectedItem = CategoryFeedsItem;
                Loaded += (_, _) => UpdateAtStartup.Focus();
                break;
            case SettingsSection.Reader:
                Title = SettingsTitle.Text = T("Setări aplicație");
                CategoryList.SelectedItem = CategoryReaderItem;
                Loaded += (_, _) => ReaderMode.Focus();
                break;
            case SettingsSection.Storage:
                Title = SettingsTitle.Text = T("Setări aplicație");
                CategoryList.SelectedItem = CategoryStorageItem;
                Loaded += (_, _) => AutoCleanup.Focus();
                break;
            case SettingsSection.NewsBlur:
                Title = SettingsTitle.Text = T("Setări aplicație");
                CategoryList.SelectedItem = CategoryNewsBlurItem;
                Loaded += (_, _) => NewsBlurSavedStoryMode.Focus();
                break;
            case SettingsSection.Application:
            default:
                Title = SettingsTitle.Text = T("Setări aplicație");
                CategoryList.SelectedItem = CategoryGeneralItem;
                Loaded += (_, _) => CategoryList.Focus();
                break;
        }
        Closed += (_, _) => _speech.Dispose();
    }

    private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryList.SelectedItem is not ListBoxItem selected) return;
        var tag = selected.Tag?.ToString();
        ShowCategory(tag);
    }

    private void ShowCategory(string? categoryTag)
    {
        GeneralCategoryPanel.Visibility = categoryTag == "General" ? Visibility.Visible : Visibility.Collapsed;
        FeedsCategoryPanel.Visibility = categoryTag == "Feeds" ? Visibility.Visible : Visibility.Collapsed;
        ReaderCategoryPanel.Visibility = categoryTag == "Reader" ? Visibility.Visible : Visibility.Collapsed;
        StorageCategoryPanel.Visibility = categoryTag == "Storage" ? Visibility.Visible : Visibility.Collapsed;
        NewsBlurCategoryPanel.Visibility = categoryTag == "NewsBlur" ? Visibility.Visible : Visibility.Collapsed;
        VoiceCategoryPanel.Visibility = categoryTag == "Voice" ? Visibility.Visible : Visibility.Collapsed;
        AiCategoryPanel.Visibility = categoryTag == "Ai" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var pendingProvider = AiProviderIds.Normalize(_pendingAiDefaultProvider);
        if (pendingProvider != AiProviderIds.Gemini && !IsAiProviderConfigured(pendingProvider))
        {
            var message = T("Activează OpenAI și introdu cheia API înainte să îl alegi ca furnizor implicit.").Replace("OpenAI", AiProviderIds.DisplayName(pendingProvider), StringComparison.Ordinal);
            MessageBox.Show(this, message, T("Furnizor AI implicit"), MessageBoxButton.OK, MessageBoxImage.Warning);
            AiProviderSelector.Focus();
            return;
        }
        SaveSettingsValues();
        DialogResult = true;
    }

    private void SaveSettingsValues()
    {
        Settings.AutoCleanupEnabled = AutoCleanup.IsChecked == true;
        SaveLanguageSetting();
        Settings.ColorScheme = ColorThemeManager.Normalize((ColorScheme.SelectedItem as ComboBoxItem)?.Tag?.ToString());
        Settings.ReaderMode = ReaderModeIds.Normalize((ReaderMode.SelectedItem as ComboBoxItem)?.Tag?.ToString());
        Settings.ArticleOpenMode = ArticleOpenModeIds.Normalize((ArticleOpenMode.SelectedItem as ComboBoxItem)?.Tag?.ToString());
        Settings.UpdateAtStartup = UpdateAtStartup.IsChecked == true;
        Settings.CheckAppUpdatesAtStartup = CheckAppUpdatesAtStartup.IsChecked == true;
        Settings.ArticleExportFolder = string.IsNullOrWhiteSpace(ArticleExportFolder.Text) ? AppSettings.DefaultArticleExportFolder : ArticleExportFolder.Text.Trim();
        Settings.ArticleExportFormat = ArticleExportService.NormalizeFormat((ArticleExportFormat.SelectedItem as ComboBoxItem)?.Tag?.ToString());
        Settings.SoundAlertsEnabled = SoundAlerts.IsChecked == true;
        Settings.SoundAlertOnSuccess = SoundAlertOnSuccess.IsChecked == true;
        Settings.SoundAlertOnNewArticles = SoundAlertOnNewArticles.IsChecked == true;
        Settings.SoundAlertOnErrors = SoundAlertOnErrors.IsChecked == true;
        Settings.HideRepeatedArticlesInGlobalViews = HideRepeatedArticles.IsChecked == true;
        Settings.NewsBlurSavedStoryMode = NormalizeNewsBlurSavedStoryMode((NewsBlurSavedStoryMode.SelectedItem as ComboBoxItem)?.Tag?.ToString());
        Settings.NewsBlurSyncFeedsAndFoldersAtStartup = NewsBlurSyncFeedsAndFoldersAtStartup.IsChecked == true;
        Settings.NewsBlurSyncArticlesAndStatesAtStartup = NewsBlurSyncArticlesAndStatesAtStartup.IsChecked == true;
        Settings.NewsBlurAutoSyncEnabled = NewsBlurAutoSyncEnabled.IsChecked == true;
        Settings.NewsBlurAutoSyncMinutes = NormalizeNewsBlurAutoSyncMinutes((NewsBlurAutoSyncMinutes.SelectedItem as ComboBoxItem)?.Tag?.ToString());
        Settings.ReadNowFavoriteDays = int.TryParse((ReadNowFavoriteDays.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out var readNowDays) ? readNowDays : 7;
        SaveSpeechSettings();
        Settings.RetentionDays = int.TryParse((RetentionDays.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out var days) ? days : 90;
        SaveAiInstructions();
        Settings.GeminiEnabled = GeminiEnabled.IsChecked == true;
        Settings.EncryptedGeminiKey = string.IsNullOrWhiteSpace(GeminiKey.Password) ? null : SecretProtector.Protect(GeminiKey.Password);
        Settings.AiDefaultProvider = AiProviderIds.Normalize(_pendingAiDefaultProvider);
        Settings.OpenAiEnabled = OpenAiEnabled.IsChecked == true;
        Settings.EncryptedOpenAiKey = string.IsNullOrWhiteSpace(OpenAiKey.Password) ? null : SecretProtector.Protect(OpenAiKey.Password);
        Settings.OpenAiModel = string.IsNullOrWhiteSpace(OpenAiModel.Text) ? "gpt-6-luna" : OpenAiModel.Text.Trim();
        Settings.MistralEnabled = MistralEnabled.IsChecked == true;
        Settings.EncryptedMistralKey = string.IsNullOrWhiteSpace(MistralKey.Password) ? null : SecretProtector.Protect(MistralKey.Password);
        Settings.MistralModel = string.IsNullOrWhiteSpace(MistralModel.Text) ? "mistral-small-latest" : MistralModel.Text.Trim();
        Settings.DeepSeekEnabled = DeepSeekEnabled.IsChecked == true;
        Settings.EncryptedDeepSeekKey = string.IsNullOrWhiteSpace(DeepSeekKey.Password) ? null : SecretProtector.Protect(DeepSeekKey.Password);
        Settings.DeepSeekModel = string.IsNullOrWhiteSpace(DeepSeekModel.Text) ? "deepseek-flash" : DeepSeekModel.Text.Trim();
        SaveGoogleTranslateSettings();
        SaveDeepLSettings();
    }
    private void SaveLanguageSetting()
    {
        var selected = UiLanguage.SelectedItem as UiLanguage;
        Settings.UiLanguage = UiCulture.NormalizeSelection(selected?.Code);
        LanguageChanged = !string.Equals(_initialLanguage, Settings.UiLanguage, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeNewsBlurSavedStoryMode(string? value) => value switch
    {
        "ReadLater" => "ReadLater",
        "Both" => "Both",
        _ => "Favorite"
    };
    private static int NormalizeNewsBlurAutoSyncMinutes(int value) => value switch
    {
        15 or 30 or 60 or 180 => value,
        _ => 30
    };
    private static int NormalizeNewsBlurAutoSyncMinutes(string? value) => int.TryParse(value, out var minutes)
        ? NormalizeNewsBlurAutoSyncMinutes(minutes)
        : 30;
    private void SaveSpeechSettings()
    {
        Settings.SpeechEngine = SelectedSpeechEngine();
        var voice = SpeechVoice.IsEnabled ? SpeechVoice.SelectedItem as SpeechVoiceChoice : null;
        if (Settings.SpeechEngine == SpeechEngineIds.EspeakNg) Settings.EspeakVoiceName = voice?.Id ?? "ro";
        else if (Settings.SpeechEngine == SpeechEngineIds.GeminiTts) Settings.GeminiVoiceName = voice?.Id ?? "Charon";
        else Settings.SpeechVoiceName = voice?.Id;
        Settings.EspeakPitch = EspeakPitch.SelectedItem is int pitch ? pitch : 50;
        if (Settings.SpeechEngine == SpeechEngineIds.EspeakNg)
        {
            Settings.EspeakVariant = (EspeakVariant.SelectedItem as SpeechVoiceChoice)?.Id ?? string.Empty;
            Settings.EspeakInflection = EspeakInflection.SelectedItem is int inflection ? inflection : 100;
        }
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
            ReadSavedGeminiKey(),
            (EspeakVariant.SelectedItem as SpeechVoiceChoice)?.Id,
            EspeakInflection.SelectedItem is int inflection ? inflection : 100);
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
        var isEspeak = engine == SpeechEngineIds.EspeakNg;
        EspeakVariantPanel.Visibility = isEspeak ? Visibility.Visible : Visibility.Collapsed;
        EspeakPitchPanel.Visibility = engine == SpeechEngineIds.EspeakNg ? Visibility.Visible : Visibility.Collapsed;
        EspeakInflectionPanel.Visibility = isEspeak ? Visibility.Visible : Visibility.Collapsed;
        GeminiSpeechNotice.Visibility = engine == SpeechEngineIds.GeminiTts ? Visibility.Visible : Visibility.Collapsed;
        EspeakVariant.Items.Clear();
        if (isEspeak)
        {
            EspeakVariant.Items.Add(new SpeechVoiceChoice(string.Empty, T("Vocea implicită a limbii")));
            foreach (var variant in _speech.InstalledVariants(engine)) EspeakVariant.Items.Add(variant);
            EspeakVariant.SelectedItem = EspeakVariant.Items.Cast<SpeechVoiceChoice>()
                .FirstOrDefault(item => string.Equals(item.Id, Settings.EspeakVariant, StringComparison.OrdinalIgnoreCase))
                ?? EspeakVariant.Items[0];
        }
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
                SpeechEngineIds.EspeakNg => F("Au fost găsite {0} voci și {1} variante eSpeak NG incluse.", SpeechVoice.Items.Count, EspeakVariant.Items.Count - 1),
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
    private void AiProviderSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializingAiProvider) return;
        var selected = SelectedAiProvider();
        AiDefaultProvider.IsChecked = string.Equals(selected, _pendingAiDefaultProvider, StringComparison.OrdinalIgnoreCase);
        UpdateAiProviderPanel();
    }

    private void AiDefaultProvider_Changed(object sender, RoutedEventArgs e)
    {
        if (_initializingAiProvider) return;
        var selected = SelectedAiProvider();
        if (AiDefaultProvider.IsChecked == true)
        {
            _pendingAiDefaultProvider = selected;
            return;
        }
        if (!string.Equals(selected, _pendingAiDefaultProvider, StringComparison.OrdinalIgnoreCase)) return;

        var alternative = AiProviderIds.All.FirstOrDefault(provider =>
            !string.Equals(provider, selected, StringComparison.OrdinalIgnoreCase) && IsAiProviderConfigured(provider));
        if (alternative is null)
        {
            _initializingAiProvider = true;
            AiDefaultProvider.IsChecked = true;
            _initializingAiProvider = false;
            MessageBox.Show(this, T("Activează și testează celălalt furnizor înainte de a-l alege ca implicit."), T("Furnizor AI implicit"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        _pendingAiDefaultProvider = alternative;
    }

    private bool IsAiProviderConfigured(string provider) => AiProviderIds.Normalize(provider) switch
    {
        AiProviderIds.OpenAI => OpenAiEnabled.IsChecked == true && !string.IsNullOrWhiteSpace(OpenAiKey.Password),
        AiProviderIds.Mistral => MistralEnabled.IsChecked == true && !string.IsNullOrWhiteSpace(MistralKey.Password),
        AiProviderIds.DeepSeek => DeepSeekEnabled.IsChecked == true && !string.IsNullOrWhiteSpace(DeepSeekKey.Password),
        _ => GeminiEnabled.IsChecked == true && !string.IsNullOrWhiteSpace(GeminiKey.Password)
    };

    private string SelectedAiProvider() => AiProviderIds.Normalize((AiProviderSelector.SelectedItem as ComboBoxItem)?.Tag?.ToString());

    private void UpdateAiProviderPanel()
    {
        var selected = SelectedAiProvider();
        GeminiProviderPanel.Visibility = selected == AiProviderIds.Gemini ? Visibility.Visible : Visibility.Collapsed;
        OpenAiProviderPanel.Visibility = selected == AiProviderIds.OpenAI ? Visibility.Visible : Visibility.Collapsed;
        MistralProviderPanel.Visibility = selected == AiProviderIds.Mistral ? Visibility.Visible : Visibility.Collapsed;
        DeepSeekProviderPanel.Visibility = selected == AiProviderIds.DeepSeek ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SaveDeepLSettings()
    {
        Settings.DeepLEnabled = DeepLEnabled.IsChecked == true;
        Settings.EncryptedDeepLKey = string.IsNullOrWhiteSpace(DeepLKey.Password) ? null : SecretProtector.Protect(DeepLKey.Password);
    }
    private void PopulateGoogleTranslateLanguages(AppSettings settings)
    {
        GoogleTranslateSourceLanguage.Items.Clear();
        GoogleTranslateSourceLanguage.Items.Add(new ComboBoxItem { Content = T("Detectare automată"), Tag = "auto" });
        foreach (var language in GoogleTranslateConnection.SupportedLanguages)
            GoogleTranslateSourceLanguage.Items.Add(new ComboBoxItem { Content = language.DisplayName, Tag = language.Code });

        GoogleTranslateTargetLanguage.Items.Clear();
        GoogleTranslateTargetLanguage.Items.Add(new ComboBoxItem { Content = T("Limba interfeței aplicației"), Tag = "ui" });
        foreach (var language in GoogleTranslateConnection.SupportedLanguages)
            GoogleTranslateTargetLanguage.Items.Add(new ComboBoxItem { Content = language.DisplayName, Tag = language.Code });

        var sourceCode = GoogleTranslateConnection.NormalizeSourceLanguage(settings.GoogleTranslateSourceLanguage);
        GoogleTranslateSourceLanguage.SelectedItem = GoogleTranslateSourceLanguage.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), sourceCode, StringComparison.OrdinalIgnoreCase))
            ?? GoogleTranslateSourceLanguage.Items[0];
        var targetCode = GoogleTranslateConnection.NormalizeTargetLanguagePreference(settings.GoogleTranslateTargetLanguage);
        GoogleTranslateTargetLanguage.SelectedItem = GoogleTranslateTargetLanguage.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), targetCode, StringComparison.OrdinalIgnoreCase))
            ?? GoogleTranslateTargetLanguage.Items[0];
    }
    private void SaveGoogleTranslateSettings()
    {
        var source = (GoogleTranslateSourceLanguage.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        var target = (GoogleTranslateTargetLanguage.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        Settings.GoogleTranslateSourceLanguage = GoogleTranslateConnection.NormalizeSourceLanguage(source);
        Settings.GoogleTranslateTargetLanguage = GoogleTranslateConnection.NormalizeTargetLanguagePreference(target);
    }
    private void SaveAiInstructions() => Settings.AiInstructions = AiInstructionsLocalization.ForStorage(AiInstructions.Text);
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
    private void OpenAiKey_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("https://platform.openai.com/api-keys") { UseShellExecute = true }); }
        catch (Exception exception) { MessageBox.Show(this, F("Pagina OpenAI pentru gestionarea cheilor API nu a putut fi deschisă. {0}", exception.Message), T("Deschidere nereușită"), MessageBoxButton.OK, MessageBoxImage.Error); }
    }
    private void MistralKey_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("https://console.mistral.ai/api-keys/") { UseShellExecute = true }); }
        catch (Exception exception)
        {
            var message = F("Pagina OpenAI pentru gestionarea cheilor API nu a putut fi deschisă. {0}", exception.Message)
                .Replace("OpenAI", AiProviderIds.Mistral, StringComparison.Ordinal);
            MessageBox.Show(this, message, T("Deschidere nereușită"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private async void TestMistral_Click(object sender, RoutedEventArgs e)
    {
        var key = MistralKey.Password;
        if (string.IsNullOrWhiteSpace(key))
        {
            MistralStatus.Text = T("Introdu mai întâi cheia API OpenAI.").Replace("OpenAI", AiProviderIds.Mistral, StringComparison.Ordinal);
            MessageBox.Show(this, MistralStatus.Text, T("Test OpenAI").Replace("OpenAI", AiProviderIds.Mistral, StringComparison.Ordinal), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MistralStatus.Text = T("Se testează conexiunea OpenAI. Nu este trimis niciun articol.").Replace("OpenAI", AiProviderIds.Mistral, StringComparison.Ordinal);
        TestMistralButton.IsEnabled = false;
        await Task.Yield();
        string result;
        MessageBoxImage icon;
        try
        {
            var models = await new MistralConnection().TestAsync(key);
            var preferred = models.FirstOrDefault(model => string.Equals(model, MistralModel.Text.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? models.FirstOrDefault(model => string.Equals(model, "mistral-small-latest", StringComparison.OrdinalIgnoreCase))
                ?? models[0];
            MistralModel.Items.Clear();
            foreach (var model in models) MistralModel.Items.Add(new ComboBoxItem { Content = model, Tag = model });
            MistralModel.Text = preferred;
            MistralEnabled.IsChecked = true;
            result = F("Conexiune OpenAI reușită. S-au găsit {0} modele accesibile. Apasă Salvează pentru păstrarea setării.", models.Count)
                .Replace("OpenAI", AiProviderIds.Mistral, StringComparison.Ordinal);
            icon = MessageBoxImage.Information;
        }
        catch (Exception exception)
        {
            result = F("Conexiunea OpenAI a eșuat: {0}", exception.Message)
                .Replace("OpenAI", AiProviderIds.Mistral, StringComparison.Ordinal);
            icon = MessageBoxImage.Warning;
        }
        finally { TestMistralButton.IsEnabled = true; }

        MistralStatus.Text = result;
        MessageBox.Show(this, result, T("Test OpenAI").Replace("OpenAI", AiProviderIds.Mistral, StringComparison.Ordinal), MessageBoxButton.OK, icon);
    }
    private void DeepSeekKey_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("https://platform.deepseek.com/api_keys") { UseShellExecute = true }); }
        catch (Exception exception)
        {
            var message = F("Pagina OpenAI pentru gestionarea cheilor API nu a putut fi deschisă. {0}", exception.Message)
                .Replace("OpenAI", AiProviderIds.DeepSeek, StringComparison.Ordinal);
            MessageBox.Show(this, message, T("Deschidere nereușită"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private async void TestDeepSeek_Click(object sender, RoutedEventArgs e)
    {
        var key = DeepSeekKey.Password;
        if (string.IsNullOrWhiteSpace(key))
        {
            DeepSeekStatus.Text = T("Introdu mai întâi cheia API OpenAI.").Replace("OpenAI", AiProviderIds.DeepSeek, StringComparison.Ordinal);
            MessageBox.Show(this, DeepSeekStatus.Text, T("Test OpenAI").Replace("OpenAI", AiProviderIds.DeepSeek, StringComparison.Ordinal), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DeepSeekStatus.Text = T("Se testează conexiunea OpenAI. Nu este trimis niciun articol.").Replace("OpenAI", AiProviderIds.DeepSeek, StringComparison.Ordinal);
        TestDeepSeekButton.IsEnabled = false;
        await Task.Yield();
        string result;
        MessageBoxImage icon;
        try
        {
            var models = await new DeepSeekConnection().TestAsync(key);
            var preferred = models.FirstOrDefault(model => string.Equals(model, DeepSeekModel.Text.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? models.FirstOrDefault(model => string.Equals(model, "deepseek-flash", StringComparison.OrdinalIgnoreCase))
                ?? models[0];
            DeepSeekModel.Items.Clear();
            foreach (var model in models) DeepSeekModel.Items.Add(new ComboBoxItem { Content = model, Tag = model });
            DeepSeekModel.Text = preferred;
            DeepSeekEnabled.IsChecked = true;
            result = F("Conexiune OpenAI reușită. S-au găsit {0} modele accesibile. Apasă Salvează pentru păstrarea setării.", models.Count)
                .Replace("OpenAI", AiProviderIds.DeepSeek, StringComparison.Ordinal);
            icon = MessageBoxImage.Information;
        }
        catch (Exception exception)
        {
            result = F("Conexiunea OpenAI a eșuat: {0}", exception.Message)
                .Replace("OpenAI", AiProviderIds.DeepSeek, StringComparison.Ordinal);
            icon = MessageBoxImage.Warning;
        }
        finally { TestDeepSeekButton.IsEnabled = true; }

        DeepSeekStatus.Text = result;
        MessageBox.Show(this, result, T("Test OpenAI").Replace("OpenAI", AiProviderIds.DeepSeek, StringComparison.Ordinal), MessageBoxButton.OK, icon);
    }
    private async void TestOpenAi_Click(object sender, RoutedEventArgs e)
    {
        var key = OpenAiKey.Password;
        if (string.IsNullOrWhiteSpace(key))
        {
            OpenAiStatus.Text = T("Introdu mai întâi cheia API OpenAI.");
            MessageBox.Show(this, OpenAiStatus.Text, T("Test OpenAI"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        OpenAiStatus.Text = T("Se testează conexiunea OpenAI. Nu este trimis niciun articol.");
        TestOpenAiButton.IsEnabled = false;
        await Task.Yield();
        string result;
        MessageBoxImage icon;
        try
        {
            var models = await new OpenAiConnection().TestAsync(key);
            var preferred = models.FirstOrDefault(model => string.Equals(model, OpenAiModel.Text.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? models.FirstOrDefault(model => string.Equals(model, "gpt-6-luna", StringComparison.OrdinalIgnoreCase))
                ?? models[0];
            OpenAiModel.Items.Clear();
            foreach (var model in models) OpenAiModel.Items.Add(new ComboBoxItem { Content = model, Tag = model });
            OpenAiModel.Text = preferred;
            OpenAiEnabled.IsChecked = true;
            result = F("Conexiune OpenAI reușită. S-au găsit {0} modele accesibile. Apasă Salvează pentru păstrarea setării.", models.Count);
            icon = MessageBoxImage.Information;
        }
        catch (Exception exception)
        {
            result = F("Conexiunea OpenAI a eșuat: {0}", exception.Message);
            icon = MessageBoxImage.Warning;
        }
        finally { TestOpenAiButton.IsEnabled = true; }

        OpenAiStatus.Text = result;
        MessageBox.Show(this, result, T("Test OpenAI"), MessageBoxButton.OK, icon);
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
        SaveSettingsValues();
        CleanupRequested = true;
        DialogResult = true;
    }
    private static string T(string source) => UiText.Translate(source);
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);
}
