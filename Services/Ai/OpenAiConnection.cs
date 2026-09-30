using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

/// <summary>OpenAI Responses API access for article conversations.</summary>
public sealed class OpenAiConnection
{
    private const string ApiRoot = "https://api.openai.com/v1";
    private const int MaximumErrorCharacters = 240;
    private static readonly HttpClient SharedClient = new() { Timeout = TimeSpan.FromSeconds(45) };
    private readonly HttpClient _client;

    public OpenAiConnection(HttpClient? client = null) => _client = client ?? SharedClient;

    /// <summary>Checks the API key and discovers usable text models without sending article content.</summary>
    public async Task<IReadOnlyList<string>> TestAsync(string key, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, $"{ApiRoot}/models", key);
        using var response = await _client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(DescribeError(response.StatusCode, body, key));

        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException(T("Răspunsul OpenAI nu conține lista modelelor."));

        var models = data.EnumerateArray()
            .Where(item => item.TryGetProperty("id", out _))
            .Select(item => item.GetProperty("id").GetString() ?? string.Empty)
            .Where(IsTextModel)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (models.Length == 0) throw new InvalidOperationException(T("Cheia OpenAI nu are modele text disponibile pentru Responses API."));
        return models;
    }

    public async Task<string> GenerateAsync(string key, string model, string instructions, string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException(T("Introdu cheia API OpenAI în Setări Inteligență artificială."));
        if (string.IsNullOrWhiteSpace(model)) throw new InvalidOperationException(T("Selectează un model OpenAI în Setări Inteligență artificială."));

        var requestBody = new
        {
            model = model.Trim(),
            instructions,
            input = prompt,
            max_output_tokens = 2048,
            store = false
        };
        using var request = CreateRequest(HttpMethod.Post, $"{ApiRoot}/responses", key);
        request.Content = JsonContent.Create(requestBody);
        using var response = await _client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(DescribeError(response.StatusCode, body, key));

        using var document = JsonDocument.Parse(body);
        if (document.RootElement.TryGetProperty("output_text", out var outputText) && outputText.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(outputText.GetString()))
            return outputText.GetString()!;

        if (document.RootElement.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array)
        {
            var text = output.EnumerateArray()
                .Where(item => item.TryGetProperty("type", out var type) && type.GetString() == "message")
                .Where(item => item.TryGetProperty("content", out _))
                .SelectMany(item => item.GetProperty("content").EnumerateArray())
                .Where(item => item.TryGetProperty("type", out var type) && type.GetString() == "output_text" && item.TryGetProperty("text", out _))
                .Select(item => item.GetProperty("text").GetString())
                .Where(value => !string.IsNullOrWhiteSpace(value));
            var result = string.Join(Environment.NewLine, text);
            if (!string.IsNullOrWhiteSpace(result)) return result;
        }

        throw new InvalidOperationException(T("OpenAI nu a returnat text."));
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string uri, string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException(T("Introdu mai întâi cheia API OpenAI."));
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key.Trim());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static bool IsTextModel(string model)
    {
        if (!(model.StartsWith("gpt-", StringComparison.OrdinalIgnoreCase) || model.StartsWith("ft:gpt-", StringComparison.OrdinalIgnoreCase))) return false;
        return !new[] { "audio", "transcribe", "embedding", "image", "tts", "realtime", "moderation", "search" }
            .Any(part => model.Contains(part, StringComparison.OrdinalIgnoreCase));
    }

    private static string DescribeError(HttpStatusCode status, string body, string key)
    {
        var safeBody = string.IsNullOrEmpty(key) ? body : body.Replace(key, "[redacted]", StringComparison.Ordinal);
        if (safeBody.Length > MaximumErrorCharacters) safeBody = safeBody[..MaximumErrorCharacters];
        if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return F("Cheia API OpenAI nu a fost acceptată. Verifică cheia și permisiunile. Detalii: {0}", safeBody);
        if ((int)status == 429)
            return F("OpenAI a limitat solicitarea sau a fost atinsă cota contului. Detalii: {0}", safeBody);
        return F("OpenAI a răspuns cu eroarea {0}. Detalii: {1}", (int)status, safeBody);
    }

    private static string T(string source) => UiText.Translate(source);
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);
}
