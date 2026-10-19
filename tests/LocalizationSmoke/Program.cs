using System.Globalization;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Resources;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CititorRSS.Jaws;
using CititorRSS.Jaws.Localization;
using CititorRSS.Jaws.Services.Content;
using CititorRSS.Jaws.Services.Update;
using OrizontSetup;

var failures = new List<string>();
var projectRoot = FindProjectRoot();
var baseResources = ReadResourceMap(Path.Combine(projectRoot, "Resources", "UiStrings.resx"));
var sourceKeys = CollectSourceKeys(projectRoot);
var compiledResources = new ResourceManager("CititorRSS.Jaws.Resources.UiStrings", typeof(UiText).Assembly);
var cultureNames = new[] { "en-US", "es-ES", "fr-FR", "de-DE", "pt-BR", "hu-HU", "it-IT" };
var settingsLabels = new Dictionary<string, (string Application, string Voice, string Ai)>(StringComparer.Ordinal)
{
    ["en-US"] = ("Application settings", "Voice settings", "Artificial intelligence settings"),
    ["es-ES"] = ("Configuración de la aplicación", "Configuración de voz", "Configuración de inteligencia artificial"),
    ["fr-FR"] = ("Paramètres de l'application", "Paramètres vocaux", "Paramètres de l’intelligence artificielle"),
    ["de-DE"] = ("Anwendungseinstellungen", "Spracheinstellungen", "Einstellungen für künstliche Intelligenz"),
    ["pt-BR"] = ("Configurações do aplicativo", "Configurações de voz", "Configurações de inteligência artificial"),
    ["hu-HU"] = ("Alkalmazásbeállítások", "Hangbeállítások", "Mesterségesintelligencia-beállítások"),
    ["it-IT"] = ("Impostazioni dell'applicazione", "Impostazioni vocali", "Impostazioni dell’intelligenza artificiale")
};
var expectedInstallerShortcutNames = new Dictionary<string, string>(StringComparer.Ordinal)
{
    ["ro-RO"] = "Dezinstalează Orizont RSS",
    ["en-US"] = "Uninstall Orizont RSS",
    ["es-ES"] = "Desinstalar Orizont RSS",
    ["fr-FR"] = "Désinstaller Orizont RSS",
    ["de-DE"] = "Orizont RSS deinstallieren",
    ["pt-BR"] = "Desinstalar o Orizont RSS",
    ["hu-HU"] = "Orizont RSS eltávolítása",
    ["it-IT"] = "Disinstalla Orizont RSS"
};
var obsoleteKeys = new[]
{
    "FereastrÄƒ",
    "MaximizeazÄƒ/restabileÈ™te fereastra",
    "MaximizeazÄƒ sau restabileÈ™te fereastra.",
    "Deschide SetÄƒri voce.",
    "Fereastra a fost maximizatÄƒ.",
    "Fereastra a fost restabilitÄƒ.",
    "Istoric stare și erori — Orizont RSS 1.3",
    "Orizont RSS 1.3"
};

Check("inventory", "C# and XAML UI strings are represented in the base resource", $"{sourceKeys.Count}/{baseResources.Count}", _ => sourceKeys.All(baseResources.ContainsKey));
Check("resources", "base resource has no duplicate keys", baseResources.Count.ToString(CultureInfo.InvariantCulture), _ => baseResources.Count > 0);
Check("resources", "base resource contains no mojibake", CountMojibake(baseResources).ToString(CultureInfo.InvariantCulture), _ => CountMojibake(baseResources) == 0);
Check("resources", "obsolete resource keys are absent", string.Join(" | ", obsoleteKeys.Where(baseResources.ContainsKey)), _ => obsoleteKeys.All(key => !baseResources.ContainsKey(key)));
Check("encoding", "detector catches typical mojibake", "fÃ¼r â€™", HasMojibake);
Check("encoding", "detector accepts valid accented text", "Alemão, über, français, español, magyar, italiano", value => !HasMojibake(value));
Check("version", "final product title", AppVersionInfo.ProductTitle, value => value == "Orizont RSS 1.6.3");
var mainWindowXaml = XDocument.Load(Path.Combine(projectRoot, "MainWindow.xaml"));
var articleReaderXaml = XDocument.Load(Path.Combine(projectRoot, "Views", "ArticleReaderWindow.xaml"));
XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
var readerEmptyState = mainWindowXaml.Descendants().FirstOrDefault(element => (string?)element.Attribute(xaml + "Name") == "ReaderEmptyState");
Check("UI", "reader empty state is above the read-only TextBox", (string?)readerEmptyState?.Attribute("Panel.ZIndex") ?? "missing", value => readerEmptyState is not null && value == "10");
foreach (var menuItemName in new[] { "ArticleGoogleTranslateMenuItem", "ContentGoogleTranslateMenuItem" })
{
    var menuItem = mainWindowXaml.Descendants().FirstOrDefault(element => (string?)element.Attribute(xaml + "Name") == menuItemName);
    Check("UI", $"{menuItemName} is wired to Google Translate", (string?)menuItem?.Attribute("Click") ?? "missing", value =>
        menuItem is not null &&
        value == "GoogleTranslate_Click" &&
        (string?)menuItem?.Attribute("Header") == "Tradu cu Google Translate (fără cheie API)" &&
        menuItem.Ancestors().Any(ancestor => (string?)ancestor.Attribute("Header") == "AI și traducere"));
}
foreach (var (header, handler) in new[]
{
    ("Salvează ca TXT", "SaveArticleAsTxt_Click"),
    ("Salvează ca RTF", "SaveArticleAsRtf_Click")
})
{
    var commands = articleReaderXaml.Descendants()
        .Where(element => (string?)element.Attribute("Header") == header)
        .ToArray();
    Check("UI", $"Cititor Orizont exposes {header} in top and context menus", commands.Length.ToString(CultureInfo.InvariantCulture), _ =>
        commands.Length == 2 && commands.All(element => (string?)element.Attribute("Click") == handler) &&
        commands.All(element => element.Parent is not null && (string?)element.Parent.Attribute("Header") == "Salvează articolul"));
}
foreach (var listName in new[] { "Feeds", "Articles" })
{
    var list = mainWindowXaml.Descendants().FirstOrDefault(element => (string?)element.Attribute(xaml + "Name") == listName);
    var handler = (string?)list?.Attribute("PreviewKeyDown") ?? string.Empty;
    Check("UI", $"{listName} list boundary earcon handler", handler, value => value == "ListBox_PreviewKeyDown_EdgeEarcon");
}

var emptyStates = new[]
{
    "Nu există feeduri. Adaugă sau importă un feed.",
    "Nu există feeduri în această vizualizare.",
    "Nu este selectat niciun feed. Selectează un feed pentru a afișa articolele.",
    "Nu există articole care să corespundă selecției și filtrelor curente.",
    "Nu este selectat niciun articol.",
    "Conținutul articolului selectat nu este afișat."
};

var installerCodes = InstallerLanguages.All.Select(texts => texts.Code).ToHashSet(StringComparer.Ordinal);
Check("installer", "all eight installer cultures are defined", string.Join(", ", installerCodes.Order(StringComparer.Ordinal)), _ =>
    installerCodes.SetEquals(expectedInstallerShortcutNames.Keys));
Check("installer", "uninstall shortcut names are unique and include the legacy Romanian name", string.Join(" | ", InstallerLanguages.UninstallShortcutNames), _ =>
    InstallerLanguages.UninstallShortcutNames.Count == expectedInstallerShortcutNames.Count &&
    InstallerLanguages.UninstallShortcutNames.Contains("Dezinstalează Orizont RSS.lnk", StringComparer.OrdinalIgnoreCase));
var installerStringProperties = typeof(InstallerTexts).GetProperties()
    .Where(property => property.PropertyType == typeof(string))
    .ToArray();
foreach (var installerTexts in InstallerLanguages.All)
{
    var emptyFields = installerStringProperties
        .Where(property => string.IsNullOrWhiteSpace((string?)property.GetValue(installerTexts)))
        .Select(property => property.Name)
        .ToArray();
    Check(installerTexts.Code, "all installer text fields are populated", string.Join(", ", emptyFields), _ => emptyFields.Length == 0);
    Check(installerTexts.Code, "localized uninstall shortcut name", installerTexts.UninstallShortcutName, value =>
        expectedInstallerShortcutNames.TryGetValue(installerTexts.Code, out var expected) && value == expected);
}
var installerMainSource = File.ReadAllText(Path.Combine(projectRoot, "packaging", "installer", "MainWindow.xaml.cs"));
Check("installer", "selected language controls the uninstall shortcut and cleanup covers every supported locale",
    $"create={installerMainSource.Contains("_texts.UninstallShortcutName", StringComparison.Ordinal)}; cleanup={installerMainSource.Contains("InstallerLanguages.UninstallShortcutNames", StringComparison.Ordinal)}",
    _ => installerMainSource.Contains("_texts.UninstallShortcutName", StringComparison.Ordinal) &&
         installerMainSource.Contains("InstallerLanguages.UninstallShortcutNames", StringComparison.Ordinal));

foreach (var cultureName in cultureNames)
{
    var culture = CultureInfo.GetCultureInfo(cultureName);
    CultureInfo.CurrentCulture = culture;
    CultureInfo.CurrentUICulture = culture;
    var installerTextsForCulture = InstallerLanguages.FromCode(InstallerLanguages.ResolveCode(culture.Name));
    Check(cultureName, "installer resolves the selected UI culture", installerTextsForCulture.Code, value => value == cultureName);
    var cultureResources = ReadResourceMap(Path.Combine(projectRoot, "Resources", $"UiStrings.{cultureName}.resx"));
    var compiledSet = compiledResources.GetResourceSet(culture, createIfNotExists: true, tryParents: false);
    var compiledKeys = compiledSet?.Cast<DictionaryEntry>().Select(entry => (string)entry.Key).ToHashSet(StringComparer.Ordinal) ?? [];
    var notCompiled = baseResources.Keys.Where(key => !compiledKeys.Contains(key)).ToArray();

    Check(cultureName, "resource table has the same complete key set as Romanian", $"{cultureResources.Count}/{baseResources.Count}", _ =>
        cultureResources.Count == baseResources.Count && baseResources.Keys.All(cultureResources.ContainsKey));
    Check(cultureName, "resource table contains no mojibake", CountMojibake(cultureResources).ToString(CultureInfo.InvariantCulture), _ => CountMojibake(cultureResources) == 0);
    Check(cultureName, "obsolete resource keys are absent", string.Join(" | ", obsoleteKeys.Where(cultureResources.ContainsKey)), _ => obsoleteKeys.All(key => !cultureResources.ContainsKey(key)));
    Check(cultureName, "compiled satellite contains every key without normalization", $"{notCompiled.Length}: {string.Join(" | ", notCompiled.Take(5))}", _ => notCompiled.Length == 0);

    var runtimeMismatches = new List<string>();
    var unchangedRomanian = new List<string>();
    foreach (var (key, sourceValue) in baseResources)
    {
        if (!cultureResources.TryGetValue(key, out var expected)) continue;
        var runtimeValue = UiText.Translate(key);
        if (!string.Equals(NormalizeLineEndings(runtimeValue), NormalizeLineEndings(expected), StringComparison.Ordinal))
            runtimeMismatches.Add($"{key.Replace("\n", "\\n", StringComparison.Ordinal)} => expected [{expected.Replace("\n", "\\n", StringComparison.Ordinal)}], runtime [{runtimeValue.Replace("\n", "\\n", StringComparison.Ordinal)}]");
        if (ContainsRomanianDiacritics(sourceValue) && string.Equals(sourceValue, expected, StringComparison.Ordinal)) unchangedRomanian.Add(key);
    }

    Check(cultureName, "runtime resolves every resource through the selected culture", $"{runtimeMismatches.Count}: {string.Join(" | ", runtimeMismatches.Take(5))}", _ => runtimeMismatches.Count == 0);
    Check(cultureName, "Romanian UI text is not left untranslated", unchangedRomanian.Count.ToString(CultureInfo.InvariantCulture), _ => unchangedRomanian.Count == 0);

    var missingSourceKeys = sourceKeys.Where(key => !cultureResources.ContainsKey(key)).ToArray();
    Check(cultureName, "all user-visible C# and XAML strings have a translation entry", $"{missingSourceKeys.Length}: {string.Join(" | ", missingSourceKeys.Take(5))}", _ => missingSourceKeys.Length == 0);

    Check(cultureName, "menu", UiText.Translate("_Ajutor"), value => value != "_Ajutor");
    var expectedSettings = settingsLabels[cultureName];
    Check(cultureName, "application-settings menu label", UiText.Translate("Setări aplicație"), value => value == expectedSettings.Application);
    Check(cultureName, "voice-settings menu label", UiText.Translate("Setări voci"), value => value == expectedSettings.Voice);
    Check(cultureName, "AI-settings menu label", UiText.Translate("Setări Inteligență artificială"), value => value == expectedSettings.Ai);
    Check(cultureName, "voice-settings window title", UiText.Translate("Setări voce"), value => value == expectedSettings.Voice);
    // These messages contain no Romanian diacritics, so the general detector
    // alone used to accept untranslated Romanian sentences.
    foreach (var message in new[] { "DeepL nu a putut traduce articolul: {0}", "DeepL nu a returnat text tradus." })
        Check(cultureName, "DeepL error translated", UiText.Translate(message), value => value != message);
    Check(cultureName, "brand", UiText.Translate("Ajutor Orizont RSS"), value => value.Contains("Orizont RSS", StringComparison.Ordinal));
    Check(cultureName, "lowercase folder placeholder", UiText.Translate("toate folderele"), value => value == UiText.Translate("Toate folderele").ToLower(culture));
    Check(cultureName, "formatted status", UiText.Format("Verificat: {0} articole disponibile.", 7), value => value.Contains('7') && !value.Contains("{0}", StringComparison.Ordinal));
    var exportPublished = new DateTimeOffset(2026, 9, 30, 10, 5, 0, TimeSpan.Zero);
    var exportArticle = new Article
    {
        Title = "Localization check",
        Content = "Article body",
        Link = "https://example.test/article",
        Published = exportPublished
    };
    var exportText = ArticleExportService.BuildText(exportArticle, exportArticle.Content, "Sample feed");
    var localizedExportLabels = new[]
    {
        UiText.Format("Data publicării: {0}", exportPublished.ToString("dd MMMM yyyy, HH:mm", culture)),
        UiText.Format("Feed: {0}", "Sample feed"),
        UiText.Translate("Conținut preluat prin Orizont RSS:"),
        UiText.Translate("Sursa articolului:")
    };
    Check(cultureName, "article export localizes dynamic labels and provenance", string.Join(" | ", localizedExportLabels), _ =>
        localizedExportLabels.All(label => exportText.Contains(label, StringComparison.Ordinal)));
    Check(cultureName, "empty article export filename uses the localized fallback", ArticleExportService.SanitizeFileName(string.Empty), value =>
        value == UiText.Translate("Articol"));
    var exportFolderError = string.Empty;
    try { ArticleExportService.Save(exportArticle, exportArticle.Content, string.Empty, "Sample feed", "txt"); }
    catch (ArgumentException exception) { exportFolderError = exception.Message; }
    Check(cultureName, "export validation error is localized", exportFolderError, value =>
        value == UiText.Translate("Folderul de export lipsește."));
    var targetLanguageError = string.Empty;
    try { await new DeepLConnection().TranslateAsync("test-key", "Article body", string.Empty); }
    catch (ArgumentException exception) { targetLanguageError = exception.Message; }
    Check(cultureName, "DeepL target-language validation error is localized", targetLanguageError, value =>
        value == UiText.Translate("Limba țintă lipsește."));
    using var updateHttpErrorClient = new HttpClient(new LocalizationHttpHandler(HttpStatusCode.ServiceUnavailable));
    var updateHttpError = await UpdateCheckerService.CheckForUpdatesAsync(updateHttpErrorClient, "https://api.github.test/releases/latest", "1.6.0");
    Check(cultureName, "HTTP update-check error is localized", updateHttpError.ErrorMessage, value =>
        value == UiText.Format("GitHub a răspuns cu codul HTTP {0}.", (int)HttpStatusCode.ServiceUnavailable));
    using var updateConnectionErrorClient = new HttpClient(new LocalizationHttpHandler(throwConnectionError: true));
    var updateConnectionError = await UpdateCheckerService.CheckForUpdatesAsync(updateConnectionErrorClient, "https://api.github.test/releases/latest", "1.6.0");
    Check(cultureName, "GitHub connection error is localized without leaking the system exception", updateConnectionError.ErrorMessage, value =>
        value == UiText.Translate("Conexiunea la GitHub nu a putut fi stabilită."));
    Check(cultureName, "eSpeak variant label", UiText.Translate("Variantă vocală eSpeak NG"), value => value != "Variantă vocală eSpeak NG");
    Check(cultureName, "eSpeak inflection label", UiText.Translate("Intonația vocii eSpeak"), value => value != "Intonația vocii eSpeak");
    foreach (var emptyState in emptyStates)
        Check(cultureName, $"empty-state translation: {emptyState}", UiText.Translate(emptyState), value => value != emptyState);

    var feed = new Feed { Name = "Exemplu", Folder = "Știri", Articles = [new Article()] };
    Check(cultureName, "feed accessible name", feed.DisplayName, value => value.Contains("Exemplu", StringComparison.Ordinal) && value.Contains('1'));

    var article = new Article { Title = "Titlu", IsFavorite = true, ReadLater = true, Tags = ["test"], SourceName = "Sursa Exemplu", IncludeSourceInDisplay = true };
    Check(cultureName, "article accessible name", article.DisplayName, value => value.Contains("Titlu", StringComparison.Ordinal) && value.Contains("test", StringComparison.Ordinal));
    Check(cultureName, "article source accessible name", article.DisplayName, value => value.Contains("Sursa Exemplu", StringComparison.Ordinal));
    Check(cultureName, "article source visual detail", article.VisualDetails, value => value.Contains("Sursa Exemplu", StringComparison.Ordinal));
    Check(cultureName, "localized guide name", UserGuideLocator.FileNameFor(culture), value => value.EndsWith($".{culture.TwoLetterISOLanguageName}.html", StringComparison.OrdinalIgnoreCase));
}

var romanian = CultureInfo.GetCultureInfo("ro-RO");
CultureInfo.CurrentCulture = romanian;
CultureInfo.CurrentUICulture = romanian;
var romanianRuntimeMismatches = baseResources.Where(entry =>
    NormalizeLineEndings(UiText.Translate(entry.Key)) != NormalizeLineEndings(entry.Value)).Select(entry => entry.Key).ToArray();
Check("ro-RO", "runtime resolves every neutral resource", string.Join(" | ", romanianRuntimeMismatches), _ => romanianRuntimeMismatches.Length == 0);
foreach (var message in new[] {
    "Verifică sesiunea NewsBlur", "Sesiunea NewsBlur nu este disponibilă.",
    "Sesiunea NewsBlur nu mai este valabilă.", "Sesiunea NewsBlur este validă.",
    "Sesiunea NewsBlur este validă. NewsBlur a raportat {0} feeduri.",
    "Contul NewsBlur a fost creat și autentificat pentru {0}. Feeduri disponibile: {1}." })
    Check("ro-RO", "NewsBlur session message remains Romanian", UiText.Translate(message), value => value == message);
Check("ro-RO", "DeepL accessible instructions are Romanian", UiText.Translate("DeepL setup instructions"), value => value == "Instrucțiuni de configurare DeepL");
foreach (var emptyState in emptyStates)
    Check("ro-RO", $"empty-state Romanian resource: {emptyState}", UiText.Translate(emptyState), value => value == emptyState);

var catalogQualityChecks = RunCatalogQualityChecks();
var (scannedWindows, scannedTexts) = RunWindowLanguageScan();
// Lista limbilor Google Translate: numele limbii în limba interfeței, cu numele nativ când diferă.
foreach (var culture in cultureNames)
{
    UiCulture.Apply(culture);
    foreach (var language in GoogleTranslateConnection.SupportedLanguages)
        Check(culture, $"translation language name is translated: {language.Name}", language.DisplayName, value => !HasRomanianLeftover(value));
}
UiCulture.Apply("en-US");
Check("en-US", "language list shows English names with native names", GoogleTranslateConnection.SupportedLanguages.First(language => language.Code == "ar").DisplayName, value => value == "Arabic (العربية)");
Check("en-US", "a language named like the interface language has no duplicate in parentheses", GoogleTranslateConnection.SupportedLanguages.First(language => language.Code == "en").DisplayName, value => value == "English");
UiCulture.Apply("ro-RO");
Check("ro-RO", "language list shows Romanian names", GoogleTranslateConnection.SupportedLanguages.First(language => language.Code == "ar").DisplayName, value => value == "Arabă (العربية)");
Check("ro-RO", "Romanian is shown once in the Romanian interface", GoogleTranslateConnection.SupportedLanguages.First(language => language.Code == "ro").DisplayName, value => value == "Română");

if (failures.Count > 0)
{
    Console.Error.WriteLine(string.Join(Environment.NewLine, failures));
    return 1;
}
Console.WriteLine($"Translation quality: {catalogQualityChecks} catalog values checked for truncation, Romanian leftovers and keyboard shortcuts; {scannedWindows} windows and {scannedTexts} visible or accessible texts checked in {cultureNames.Length} languages.");

Console.WriteLine($"Localization smoke passed: {baseResources.Count} resources and {sourceKeys.Count} source UI strings, ro-RO plus {string.Join(", ", cultureNames)}.");
return 0;

void Check(string culture, string test, string value, Func<string, bool> predicate)
{
    if (!predicate(value)) failures.Add($"{culture}: {test} failed; value: {value}");
}

// Calitatea traducerilor din catalog: valori trunchiate (de exemplu „A” pentru „Toate folderele”),
// cuvinte românești rămase și combinații de taste schimbate față de textul sursă.
int RunCatalogQualityChecks()
{
    var shortcut = new Regex(@"(?:(?:Ctrl|Alt|Shift|Insert)\+)+[A-Z0-9]+\b|\bF(?:[1-9]|1[0-2])\b");
    static string NormalizeKeys(string value) =>
        Regex.Replace(Regex.Replace(value, @"\b(Strg|Contrôle|Control)\+", "Ctrl+"), @"\b(Umschalt|Maj|Mayús|Maiusc)\+", "Shift+");
    var checkedValues = 0;
    foreach (var culture in cultureNames)
    {
        var map = ReadResourceMap(Path.Combine(projectRoot, "Resources", $"UiStrings.{culture}.resx"));
        foreach (var (key, value) in map)
        {
            checkedValues++;
            if (key.Length >= 10 && Regex.IsMatch(key, @"\p{L}{3}"))
                Check(culture, $"translation is not truncated: {key}", value, translated => translated.Trim().Length * 3 >= key.Length);
            Check(culture, $"translation has no Romanian words left: {key}", value, translated => !HasRomanianLeftover(translated));
            var shortcutKeys = shortcut.Matches(key).Select(match => match.Value).OrderBy(item => item, StringComparer.Ordinal).ToList();
            if (shortcutKeys.Count > 0)
                Check(culture, $"translation keeps the keyboard shortcuts of: {key}", value, translated =>
                    shortcut.Matches(NormalizeKeys(translated)).Select(match => match.Value).OrderBy(item => item, StringComparer.Ordinal).SequenceEqual(shortcutKeys));
        }
    }
    return checkedValues;
}

// Fiecare fereastră este construită cu date de probă (fără afișare, profil sau rețea), tradusă ca în aplicație,
// apoi sunt citite titlurile, butoanele, meniurile (inclusiv cele contextuale), numele și ajutorul accesibil.
(int Windows, int Texts) RunWindowLanguageScan()
{
    var windows = 0;
    var texts = 0;
    Exception? fatal = null;
    var thread = new Thread(() =>
    {
        try
        {
            var app = new App();
            app.InitializeComponent();
            var feed = new Feed { Name = "Demo", Url = "https://demo.example.test/rss" };
            var article = new Article { Id = "a", Title = "Demo", Content = "Demo", Link = "https://demo.example.test/a", DisplayIndex = 3, DisplayTotal = 50 };
            feed.Articles.Add(article);
            var windowTypes = typeof(App).Assembly.GetTypes().Where(type => typeof(System.Windows.Window).IsAssignableFrom(type) && !type.IsAbstract).OrderBy(type => type.Name).ToList();
            foreach (var culture in cultureNames)
            {
                UiCulture.Apply(culture);
                foreach (var model in new[] { feed.DisplayName, feed.VisualDetails, article.DisplayName, article.StatusTitlePosition, article.VisualState })
                    Check(culture, "feed and article texts read by the screen reader are translated", model, value => !HasRomanianLeftover(value));
                foreach (var type in windowTypes)
                {
                    var window = CreateSample(type, feed, article, 0) as System.Windows.Window;
                    Check(culture, $"window can be built for the language scan: {type.Name}", window is null ? "not built" : "built", value => value == "built");
                    if (window is null) continue;
                    if (culture == cultureNames[0]) windows++;
                    UiLocalizer.Apply(window);
                    foreach (var (where, text) in CollectWindowTexts(window))
                    {
                        texts++;
                        Check(culture, $"{type.Name} {where} is translated", text, value => !HasRomanianLeftover(value));
                    }
                }
            }
        }
        catch (Exception exception)
        {
            fatal = exception;
        }
    });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();
    if (fatal is not null) failures.Add($"window language scan stopped: {fatal}");
    return (windows, texts);
}

// Cuvinte românești rămase netraduse. Literele â și î există și în franceză sau portugheză, deci nu sunt folosite.
// Excepții: numele autorului și numele limbii române din listele de limbi.
static bool HasRomanianLeftover(string value)
{
    var text = Regex.Replace(value, @"Fri[șş]an|\(Română\)", string.Empty);
    return Regex.IsMatch(text, "[ăĂșȘțȚşŞţŢ]") ||
        Regex.IsMatch(text, @"(?i)(?<![\p{L}])(și|sau|pentru|sunt|nu există|articole|articolul|feeduri|feedul|feedurile|folderul|setări|setările|alege|apasă|selectat|citește|din (?:\d|\{))(?![\p{L}])");
}

static List<(string Where, string Text)> CollectWindowTexts(System.Windows.Window window)
{
    var texts = new List<(string, string)>();
    var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
    void Add(string where, object? value) { if (value is string text && text.Trim().Length > 0) texts.Add((where, text)); }
    void Walk(object node)
    {
        if (!seen.Add(node)) return;
        if (node is System.Windows.Window current) Add("title", current.Title);
        if (node is System.Windows.Controls.HeaderedItemsControl headeredItems) Add("menu or header", headeredItems.Header);
        if (node is System.Windows.Controls.HeaderedContentControl headeredContent) Add("header", headeredContent.Header);
        if (node is System.Windows.Controls.ContentControl content && node is not System.Windows.Window) Add(node.GetType().Name, content.Content);
        if (node is System.Windows.Controls.TextBlock textBlock) Add("text", textBlock.Text);
        if (node is System.Windows.DependencyObject element)
        {
            Add("accessible name", System.Windows.Automation.AutomationProperties.GetName(element));
            Add("accessible help", System.Windows.Automation.AutomationProperties.GetHelpText(element));
        }
        if (node is System.Windows.FrameworkElement framework)
        {
            Add("tooltip", framework.ToolTip);
            if (framework.ContextMenu is not null) Walk(framework.ContextMenu);
        }
        if (node is System.Windows.Controls.ItemsControl items)
            foreach (var item in items.Items) { if (item is string text) Add("list item", text); else if (item is not null) Walk(item); }
        if (node is System.Windows.DependencyObject parent)
            foreach (var child in System.Windows.LogicalTreeHelper.GetChildren(parent)) Walk(child);
    }
    Walk(window);
    return texts;
}

// Date de probă pentru constructori: setări implicite, un feed, un articol, liste cu un element și delegați goi.
static object? CreateSample(Type type, Feed feed, Article article, int depth)
{
    if (type == typeof(string)) return "Demo";
    if (type == typeof(AppSettings)) return new AppSettings();
    if (type == typeof(Feed)) return feed;
    if (type == typeof(Article)) return article;
    if (type.IsAssignableFrom(typeof(List<Feed>))) return new List<Feed> { feed };
    if (type.IsAssignableFrom(typeof(List<string>))) return new List<string> { "Demo" };
    if (type.IsAssignableFrom(typeof(List<ArticleRule>))) return new List<ArticleRule>();
    if (type.IsAssignableFrom(typeof(Dictionary<string, string>))) return new Dictionary<string, string>();
    if (type == typeof(Uri)) return new Uri("https://demo.example.test/");
    if (type == typeof(Version)) return new Version(9, 9, 9);
    if (type.IsEnum) return Enum.GetValues(type).GetValue(0);
    if (type.IsValueType) return Activator.CreateInstance(type);
    if (type.IsGenericType && type.IsInterface && type.GetGenericArguments().Length == 1)
    {
        var elementType = type.GetGenericArguments()[0];
        var list = Array.CreateInstance(elementType, 1);
        list.SetValue(CreateSample(elementType, feed, article, depth + 1), 0);
        if (type.IsAssignableFrom(list.GetType())) return list;
    }
    if (type.IsArray)
    {
        var array = Array.CreateInstance(type.GetElementType()!, 1);
        array.SetValue(CreateSample(type.GetElementType()!, feed, article, depth + 1), 0);
        return array;
    }
    if (typeof(Delegate).IsAssignableFrom(type))
    {
        var invoke = type.GetMethod("Invoke")!;
        var parameters = invoke.GetParameters().Select(parameter => System.Linq.Expressions.Expression.Parameter(parameter.ParameterType)).ToArray();
        return System.Linq.Expressions.Expression.Lambda(type, System.Linq.Expressions.Expression.Default(invoke.ReturnType), parameters).Compile();
    }
    if (depth > 3 || type.IsAbstract || type.IsInterface) return null;
    foreach (var constructor in type.GetConstructors().OrderBy(constructor => constructor.GetParameters().Length))
    {
        try
        {
            return constructor.Invoke(constructor.GetParameters().Select(parameter => parameter.HasDefaultValue ? parameter.DefaultValue : CreateSample(parameter.ParameterType, feed, article, depth + 1)).ToArray());
        }
        catch
        {
            // Se încearcă următorul constructor.
        }
    }
    return null;
}

static string FindProjectRoot()
{
    for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        if (File.Exists(Path.Combine(directory.FullName, "CititorRSS.Jaws.csproj"))) return directory.FullName;

    throw new DirectoryNotFoundException("Could not locate CititorRSS.Jaws.csproj from the smoke-test output directory.");
}

static Dictionary<string, string> ReadResourceMap(string path)
{
    var document = XDocument.Load(path);
    var resources = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var entry in document.Root?.Elements("data") ?? [])
    {
        var key = (string?)entry.Attribute("name") ?? throw new InvalidDataException($"Resource without a name in {path}.");
        var value = (string?)entry.Element("value") ?? string.Empty;
        if (!resources.TryAdd(key, value)) throw new InvalidDataException($"Duplicate resource key in {path}: {key}");
    }

    return resources;
}

static HashSet<string> CollectSourceKeys(string projectRoot)
{
    var keys = new HashSet<string>(StringComparer.Ordinal);
    var callPattern = new Regex("(?:UiText\\.(?:Translate|Format)|\\bT|\\bF|\\bSay)\\(\\s*\"((?:\\\\.|[^\"\\\\])*)\"", RegexOptions.CultureInvariant);
    var excludedSegments = new HashSet<string>(["bin", "obj", "tests", "packaging", "tools"], StringComparer.OrdinalIgnoreCase);

    foreach (var path in Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories))
    {
        var relative = Path.GetRelativePath(projectRoot, path);
        if (relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(excludedSegments.Contains)) continue;
        foreach (Match match in callPattern.Matches(File.ReadAllText(path, System.Text.Encoding.UTF8)))
        {
            var key = Regex.Unescape(match.Groups[1].Value);
            keys.Add(key == "toate folderele" ? "Toate folderele" : key);
        }
    }

    var xamlAttributes = new HashSet<string>(["Header", "Text", "Content", "ToolTip", "Title", "AutomationProperties.Name", "AutomationProperties.HelpText"], StringComparer.Ordinal);
    foreach (var path in Directory.EnumerateFiles(projectRoot, "*.xaml", SearchOption.AllDirectories))
    {
        var relative = Path.GetRelativePath(projectRoot, path);
        if (relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(excludedSegments.Contains)) continue;
        var document = XDocument.Load(path);
        foreach (var attribute in document.Descendants().Attributes())
        {
            if (!xamlAttributes.Contains(attribute.Name.ToString())) continue;
            var value = attribute.Value;
            if (!string.IsNullOrWhiteSpace(value) && value.TrimStart().StartsWith('{')) continue;
            keys.Add(value == "toate folderele" ? "Toate folderele" : value);
        }
    }

    return keys;
}

static bool ContainsRomanianDiacritics(string value) => value.Any(character => character is 'ă' or 'â' or 'î' or 'ș' or 'ț' or 'Ă' or 'Â' or 'Î' or 'Ș' or 'Ț');

static int CountMojibake(Dictionary<string, string> resources)
{
    return resources.Count(entry => HasMojibake(entry.Key) || HasMojibake(entry.Value));
}

static bool HasMojibake(string value) => Regex.IsMatch(value, "(?:Ã[^\\x00-\\x7F]|Â[^\\x00-\\x7F]|Äƒ|È™|È›|â€[\\p{L}\\p{N}\\p{P}\\p{S}]|â†[\\p{L}\\p{N}\\p{P}\\p{S}]|â€¦|ï¿½|\\uFFFD)", RegexOptions.CultureInvariant);

static string NormalizeLineEndings(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

sealed class LocalizationHttpHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly bool _throwConnectionError;

    public LocalizationHttpHandler(HttpStatusCode statusCode = HttpStatusCode.OK, bool throwConnectionError = false)
    {
        _statusCode = statusCode;
        _throwConnectionError = throwConnectionError;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_throwConnectionError) throw new HttpRequestException("synthetic network failure");
        return Task.FromResult(new HttpResponseMessage(_statusCode));
    }
}
