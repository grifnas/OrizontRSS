using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

/// <summary>
/// Best-effort, no-key translation through the endpoint used by Google Translate's web client.
/// This is not the authenticated Google Cloud Translation API.
/// </summary>
public sealed class GoogleTranslateConnection
{
    private const int MaximumChunkCharacters = 4000;
    private const string Endpoint = "https://translate.googleapis.com/translate_a/single";
    private static readonly HttpClient SharedClient = new() { Timeout = TimeSpan.FromSeconds(45) };
    private readonly HttpClient _client;

    public static IReadOnlyList<GoogleTranslateLanguage> SupportedLanguages { get; } =
    [
        // Numele românesc este cheia de traducere; numele nativ rămâne între paranteze.
        new("ar", "Arabă", "العربية"), new("bg", "Bulgară", "Български"), new("ca", "Catalană", "Català"),
        new("zh-CN", "Chineză simplificată", "简体中文"), new("zh-TW", "Chineză tradițională", "繁體中文"),
        new("hr", "Croată", "Hrvatski"), new("cs", "Cehă", "Čeština"), new("da", "Daneză", "Dansk"),
        new("nl", "Olandeză", "Nederlands"), new("en", "Engleză", "English"), new("et", "Estonă", "Eesti"),
        new("fi", "Finlandeză", "Suomi"), new("fr", "Franceză", "Français"), new("de", "Germană", "Deutsch"),
        new("el", "Greacă", "Ελληνικά"), new("he", "Ebraică", "עברית"), new("hi", "Hindi", "हिन्दी"),
        new("hu", "Maghiară", "Magyar"), new("id", "Indoneziană", "Bahasa Indonesia"), new("it", "Italiană", "Italiano"),
        new("ja", "Japoneză", "日本語"), new("ko", "Coreeană", "한국어"), new("lv", "Letonă", "Latviešu"),
        new("lt", "Lituaniană", "Lietuvių"), new("ms", "Malaeză", "Bahasa Melayu"), new("no", "Norvegiană", "Norsk"),
        new("pl", "Poloneză", "Polski"), new("pt", "Portugheză", "Português"), new("ro", "Română", "Română"),
        new("ru", "Rusă", "Русский"), new("sk", "Slovacă", "Slovenčina"), new("sl", "Slovenă", "Slovenščina"),
        new("es", "Spaniolă", "Español"), new("sv", "Suedeză", "Svenska"), new("th", "Thailandeză", "ไทย"),
        new("tr", "Turcă", "Türkçe"), new("uk", "Ucraineană", "Українська"), new("vi", "Vietnameză", "Tiếng Việt")
    ];

    public GoogleTranslateConnection(HttpClient? client = null) => _client = client ?? SharedClient;

    public async Task<string> TranslateAsync(string text, string targetLanguage, string sourceLanguage = "auto", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        if (string.IsNullOrWhiteSpace(targetLanguage))
            throw new ArgumentException(T("Limba țintă pentru traducere lipsește."), nameof(targetLanguage));

        var source = NormalizeSourceLanguage(sourceLanguage);
        var target = NormalizeTargetLanguage(targetLanguage);

        var chunks = SplitText(text, MaximumChunkCharacters);
        var translatedChunks = new List<string>(chunks.Count);
        foreach (var chunk in chunks)
        {
            var uri = $"{Endpoint}?client=gtx&sl={Uri.EscapeDataString(source)}&tl={Uri.EscapeDataString(target)}&dt=t";
            using var content = new FormUrlEncodedContent([new KeyValuePair<string, string>("q", chunk.Text)]);
            using var response = await _client.PostAsync(uri, content, cancellationToken);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                throw new InvalidOperationException(T("Google Translate a limitat cererile. Încearcă din nou mai târziu."));
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(F("Google Translate nu a putut traduce articolul: {0}", (int)response.StatusCode));

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            translatedChunks.Add(ParseResponse(body));
        }

        var result = new StringBuilder();
        for (var i = 0; i < chunks.Count; i++)
        {
            if (i > 0) result.Append(chunks[i].SeparatorBefore);
            result.Append(translatedChunks[i]);
        }
        return result.ToString();
    }

    public static string TargetLanguageForUi(CultureInfo? culture = null)
    {
        var language = (culture ?? CultureInfo.CurrentUICulture).TwoLetterISOLanguageName.ToLowerInvariant();
        return language is "ro" or "en" or "es" or "fr" or "de" or "pt" or "hu" or "it" ? language : "en";
    }

    public static string ResolveTargetLanguage(string? selection, CultureInfo? culture = null)
    {
        if (string.Equals(selection, "ui", StringComparison.OrdinalIgnoreCase)) return TargetLanguageForUi(culture);
        var normalized = NormalizeSourceLanguage(selection);
        return normalized == "auto" ? TargetLanguageForUi(culture) : normalized;
    }

    public static string NormalizeTargetLanguagePreference(string? language)
    {
        if (string.Equals(language, "ui", StringComparison.OrdinalIgnoreCase)) return "ui";
        var normalized = NormalizeSourceLanguage(language);
        return normalized == "auto" ? "ui" : normalized;
    }

    public static string NormalizeSourceLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language) || string.Equals(language, "auto", StringComparison.OrdinalIgnoreCase)) return "auto";
        return SupportedLanguages.FirstOrDefault(item => string.Equals(item.Code, language.Trim(), StringComparison.OrdinalIgnoreCase))?.Code ?? "auto";
    }

    public static string NormalizeTargetLanguage(string? language)
    {
        if (string.Equals(language, "ui", StringComparison.OrdinalIgnoreCase)) return TargetLanguageForUi();
        var normalized = NormalizeSourceLanguage(language);
        return normalized == "auto" ? TargetLanguageForUi() : normalized;
    }

    private static string ParseResponse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0 || root[0].ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException(T("Răspunsul Google Translate nu conține text tradus."));

            var result = new StringBuilder();
            foreach (var segment in root[0].EnumerateArray())
            {
                if (segment.ValueKind == JsonValueKind.Array && segment.GetArrayLength() > 0 &&
                    segment[0].ValueKind == JsonValueKind.String)
                    result.Append(segment[0].GetString());
            }

            if (result.Length == 0)
                throw new InvalidOperationException(T("Răspunsul Google Translate nu conține text tradus."));
            return result.ToString();
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(T("Răspunsul Google Translate nu a putut fi citit."), exception);
        }
    }

    private static List<TranslationChunk> SplitText(string text, int maximumLength)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var chunks = new List<TranslationChunk>();
        var current = new StringBuilder();
        var currentSeparator = string.Empty;
        var hasPreviousParagraph = false;

        foreach (var paragraph in normalized.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                if (hasPreviousParagraph && current.Length > 0) current.Append('\n');
                continue;
            }

            var remaining = paragraph.Trim();
            var isFirstPiece = true;
            while (remaining.Length > 0)
            {
                var pieceLength = Math.Min(remaining.Length, maximumLength);
                if (pieceLength < remaining.Length)
                {
                    var whitespace = remaining.LastIndexOfAny([' ', '\t'], pieceLength - 1, pieceLength);
                    if (whitespace > 0) pieceLength = whitespace;
                }

                var piece = remaining[..pieceLength].TrimEnd();
                remaining = remaining[pieceLength..].TrimStart();
                var separator = isFirstPiece ? (hasPreviousParagraph ? "\n" : string.Empty) : " ";

                if (current.Length > 0 && current.Length + separator.Length + piece.Length > maximumLength)
                {
                    chunks.Add(new TranslationChunk(current.ToString(), currentSeparator));
                    current.Clear();
                    currentSeparator = separator;
                    separator = string.Empty;
                }
                else if (current.Length == 0)
                {
                    currentSeparator = separator;
                    separator = string.Empty;
                }

                if (separator.Length > 0) current.Append(separator);
                current.Append(piece);
                isFirstPiece = false;
            }
            hasPreviousParagraph = true;
        }

        if (current.Length > 0) chunks.Add(new TranslationChunk(current.ToString(), currentSeparator));
        return chunks;
    }

    private static string T(string source) => UiText.Translate(source);
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);

    private sealed record TranslationChunk(string Text, string SeparatorBefore);
}

/// <summary>O limbă Google Translate: numele se traduce în limba interfeței, iar numele nativ se adaugă dacă diferă.</summary>
public sealed record GoogleTranslateLanguage(string Code, string Name, string NativeName)
{
    public string DisplayName
    {
        get
        {
            var name = UiText.Translate(Name);
            return string.Equals(name, NativeName, StringComparison.CurrentCultureIgnoreCase) ? name : $"{name} ({NativeName})";
        }
    }
}
