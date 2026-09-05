using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

/// <summary>Provides the optional DeepL API Free integration for article translation.</summary>
public sealed class DeepLConnection
{
    private const string Endpoint = "https://api-free.deepl.com";
    private const int MaximumChunkCharacters = 45000;

    public async Task<DeepLUsage> TestAsync(string key)
    {
        using var client = CreateClient(key);
        using var response = await client.GetAsync($"{Endpoint}/v2/usage");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(DescribeError(response.StatusCode, body));
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        return new DeepLUsage(
            root.TryGetProperty("character_count", out var count) ? count.GetInt64() : 0,
            root.TryGetProperty("character_limit", out var limit) ? limit.GetInt64() : 0);
    }

    public async Task<string> TranslateAsync(string key, string text, string targetLanguage, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        if (string.IsNullOrWhiteSpace(targetLanguage)) throw new ArgumentException("Limba țintă lipsește.", nameof(targetLanguage));
        using var client = CreateClient(key);
        var chunks = SplitText(text);
        var translated = new List<string>(chunks.Count);
        foreach (var chunk in chunks)
        {
            var request = new Dictionary<string, object?>
            {
                ["text"] = new[] { chunk },
                ["target_lang"] = targetLanguage.ToUpperInvariant(),
                ["preserve_formatting"] = true
            };
            using var response = await client.PostAsJsonAsync($"{Endpoint}/v2/translate", request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException(DescribeError(response.StatusCode, body));
            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("translations", out var translations) || translations.GetArrayLength() == 0 ||
                !translations[0].TryGetProperty("text", out var value) || string.IsNullOrWhiteSpace(value.GetString()))
                throw new InvalidOperationException(T("DeepL nu a returnat text tradus."));
            translated.Add(value.GetString()!);
        }
        return string.Join(Environment.NewLine, translated);
    }

    public static string TargetLanguageForUi()
    {
        return System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant() switch
        {
            "de" => "DE",
            "en" => "EN-US",
            "es" => "ES",
            "fr" => "FR",
            "pt" => "PT-BR",
            "ro" => "RO",
            _ => "EN-US"
        };
    }

    private static HttpClient CreateClient(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException(T("Introdu cheia API DeepL în Setări Inteligență artificială."));
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"DeepL-Auth-Key {key.Trim()}");
        return client;
    }

    private static List<string> SplitText(string text)
    {
        if (text.Length <= MaximumChunkCharacters) return [text];
        var chunks = new List<string>();
        var current = new System.Text.StringBuilder();
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (line.Length > MaximumChunkCharacters)
            {
                if (current.Length > 0) { chunks.Add(current.ToString()); current.Clear(); }
                for (var offset = 0; offset < line.Length; offset += MaximumChunkCharacters)
                    chunks.Add(line.Substring(offset, Math.Min(MaximumChunkCharacters, line.Length - offset)));
                continue;
            }
            if (current.Length + line.Length + 1 > MaximumChunkCharacters)
            {
                chunks.Add(current.ToString());
                current.Clear();
            }
            if (current.Length > 0) current.Append('\n');
            current.Append(line);
        }
        if (current.Length > 0) chunks.Add(current.ToString());
        return chunks;
    }

    private static string DescribeError(HttpStatusCode status, string body)
    {
        var detail = body.Length > 240 ? body[..240] : body;
        if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return F("Cheia API DeepL nu a fost acceptată. Verifică cheia și abonamentul API Free. Detalii: {0}", detail);
        if ((int)status is 429 or 456)
            return F("DeepL a limitat solicitarea sau limita de caractere a fost atinsă. Detalii: {0}", detail);
        return F("DeepL a răspuns cu eroarea {0}. Detalii: {1}", (int)status, detail);
    }

    private static string T(string source) => UiText.Translate(source);
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);
}

public readonly record struct DeepLUsage(long CharacterCount, long CharacterLimit);
