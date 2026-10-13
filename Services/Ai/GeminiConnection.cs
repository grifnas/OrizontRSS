using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public sealed class GeminiConnection
{
    private const string ApiRoot = "https://generativelanguage.googleapis.com/v1beta";
    // Modelele Gemini recente consumă din această limită și pentru gândirea internă, înainte de răspuns.
    public const int MaximumOutputTokens = 8192;
    private static readonly HttpClient SharedClient = new() { Timeout = TimeSpan.FromSeconds(45) };
    private static readonly object ModelCacheLock = new();
    private static string? _cachedModelKey;
    private static string? _cachedModel;
    private readonly HttpClient _client;

    public GeminiConnection(HttpClient? client = null) => _client = client ?? SharedClient;

    public async Task TestAsync(string key, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));
        using var request = CreateRequest(HttpMethod.Get, $"{ApiRoot}/models?pageSize=1", key);
        using var response = await _client.SendAsync(request, timeout.Token);
        if (response.IsSuccessStatusCode) return;

        var detail = await response.Content.ReadAsStringAsync(timeout.Token);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new InvalidOperationException(T("Cheia API Gemini nu a fost acceptată. Creează sau verifică cheia în Google AI Studio."));
        if ((int)response.StatusCode == 429)
            throw new InvalidOperationException(T("Gemini a limitat temporar solicitarea sau a fost atinsă cota contului."));
        throw new InvalidOperationException(F("Gemini a răspuns cu eroarea {0}. {1}", (int)response.StatusCode, ShortDetail(detail, key)));
    }

    public async Task<string> GenerateAsync(string key, string systemInstruction, string prompt, CancellationToken cancellationToken = default)
    {
        var model = await GetTextModelAsync(key, cancellationToken);
        var (status, body) = await SendGenerateAsync(key, model, systemInstruction, prompt, cancellationToken);
        if (status == HttpStatusCode.NotFound)
        {
            // Modelul păstrat în sesiune nu mai este disponibil: se alege din nou o singură dată.
            ForgetModel();
            model = await GetTextModelAsync(key, cancellationToken);
            (status, body) = await SendGenerateAsync(key, model, systemInstruction, prompt, cancellationToken);
        }
        if ((int)status is < 200 or > 299)
            throw new InvalidOperationException(DescribeRequestError(status, body, key));
        return ReadResponseText(body);
    }

    /// <summary>Extrage textul și explică de ce lipsește sau este incomplet, după motivul de oprire Gemini.</summary>
    public static string ReadResponseText(string body)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        if (root.TryGetProperty("promptFeedback", out var feedback) && feedback.TryGetProperty("blockReason", out var blockReason))
            throw new InvalidOperationException(F("Gemini a refuzat cererea prin filtrele de siguranță (motiv: {0}).", blockReason.GetString()));
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array || candidates.GetArrayLength() == 0)
            throw new InvalidOperationException(T("Gemini nu a returnat niciun răspuns."));

        var candidate = candidates[0];
        var finishReason = candidate.TryGetProperty("finishReason", out var reason) ? reason.GetString() ?? string.Empty : string.Empty;
        var text = string.Empty;
        if (candidate.TryGetProperty("content", out var content) && content.TryGetProperty("parts", out var parts) && parts.ValueKind == JsonValueKind.Array)
        {
            text = string.Join(Environment.NewLine, parts.EnumerateArray()
                .Where(part => !(part.TryGetProperty("thought", out var thought) && thought.ValueKind == JsonValueKind.True))
                .Where(part => part.TryGetProperty("text", out _))
                .Select(part => part.GetProperty("text").GetString()));
        }

        var hasText = !string.IsNullOrWhiteSpace(text);
        switch (finishReason)
        {
            case "MAX_TOKENS" when hasText:
                return text.TrimEnd() + Environment.NewLine + Environment.NewLine + T("[Răspunsul Gemini a fost tăiat la limita de lungime. Poți cere continuarea sau un răspuns mai scurt.]");
            case "MAX_TOKENS":
                throw new InvalidOperationException(T("Gemini a atins limita de lungime înainte să scrie răspunsul. Încearcă o cerere mai scurtă sau un articol mai scurt."));
            case "SAFETY" or "BLOCKLIST" or "PROHIBITED_CONTENT" or "SPII" or "IMAGE_SAFETY":
                throw new InvalidOperationException(F("Gemini a blocat răspunsul prin filtrele de siguranță (motiv: {0}).", finishReason));
            case "RECITATION":
                throw new InvalidOperationException(T("Gemini a oprit răspunsul deoarece reproducea un text protejat. Cere un rezumat cu propriile cuvinte."));
        }
        if (hasText) return text;
        throw new InvalidOperationException(F("Gemini nu a returnat text (motiv: {0}).", string.IsNullOrWhiteSpace(finishReason) ? "?" : finishReason));
    }

    private async Task<(HttpStatusCode Status, string Body)> SendGenerateAsync(string key, string model, string systemInstruction, string prompt, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            systemInstruction = new { parts = new[] { new { text = systemInstruction } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
            generationConfig = new { temperature = 0.3, maxOutputTokens = MaximumOutputTokens }
        };
        using var request = CreateRequest(HttpMethod.Post, $"{ApiRoot}/{model}:generateContent", key);
        request.Content = JsonContent.Create(requestBody);
        using var response = await _client.SendAsync(request, cancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    private async Task<string> GetTextModelAsync(string key, CancellationToken cancellationToken)
    {
        var keyId = KeyId(key);
        lock (ModelCacheLock)
        {
            if (_cachedModel is not null && _cachedModelKey == keyId) return _cachedModel;
        }

        using var request = CreateRequest(HttpMethod.Get, $"{ApiRoot}/models?pageSize=100", key);
        using var response = await _client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(F("Nu s-au putut obține modelele Gemini: {0}. {1}", (int)response.StatusCode, ShortDetail(body, key)));
        using var document = JsonDocument.Parse(body);
        var models = document.RootElement.GetProperty("models").EnumerateArray()
            .Where(item => item.TryGetProperty("supportedGenerationMethods", out var methods) && methods.EnumerateArray().Any(method => method.GetString() == "generateContent"))
            .Select(item => item.GetProperty("name").GetString() ?? string.Empty).ToList();
        var preferred = new[] { "models/gemini-3.5-flash", "models/gemini-2.5-flash", "models/gemini-2.0-flash" };
        var model = preferred.FirstOrDefault(models.Contains) ?? models.FirstOrDefault() ?? throw new InvalidOperationException(T("Cheia Gemini nu are niciun model disponibil pentru generarea de text."));
        lock (ModelCacheLock)
        {
            _cachedModelKey = keyId;
            _cachedModel = model;
        }
        return model;
    }

    public static void ForgetModel()
    {
        lock (ModelCacheLock)
        {
            _cachedModelKey = null;
            _cachedModel = null;
        }
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string key)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("x-goog-api-key", key.Trim());
        return request;
    }

    // Cheia nu este păstrată în clar; se reține doar o amprentă pentru a ști când s-a schimbat.
    private static string KeyId(string key) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key.Trim())));

    private static string ShortDetail(string detail, string key)
    {
        if (!string.IsNullOrWhiteSpace(key)) detail = detail.Replace(key.Trim(), "***", StringComparison.Ordinal);
        return detail.Length > 240 ? detail[..240] : detail;
    }

    private static string DescribeRequestError(HttpStatusCode status, string body, string key)
    {
        var detail = ShortDetail(body, key);
        if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) return F("Gemini a refuzat comanda. Verifică cheia API, proiectul Google și permisiunile. Detalii server: {0}", detail);
        if ((int)status == 429) return F("Gemini a limitat temporar comanda sau a fost atinsă cota contului. Detalii server: {0}", detail);
        return F("Gemini a răspuns cu eroarea {0}. Detalii server: {1}", (int)status, detail);
    }
    private static string T(string source) => UiText.Translate(source);
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);
}
