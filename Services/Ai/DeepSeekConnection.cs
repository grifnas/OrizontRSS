using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

/// <summary>DeepSeek Chat Completions access with a model-list-only connection test.</summary>
public sealed class DeepSeekConnection
{
    private const string ApiRoot = "https://api.deepseek.com";
    private const int MaximumErrorCharacters = 240;
    private static readonly HttpClient SharedClient = new() { Timeout = TimeSpan.FromSeconds(45) };
    private readonly HttpClient _client;

    public DeepSeekConnection(HttpClient? client = null) => _client = client ?? SharedClient;

    /// <summary>Checks the key and discovers available models without sending article text.</summary>
    public async Task<IReadOnlyList<string>> TestAsync(string key, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, $"{ApiRoot}/models", key);
        using var response = await _client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(DescribeError(response.StatusCode, body, key));

        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException(ProviderText("Răspunsul OpenAI nu conține lista modelelor."));

        var models = data.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.Object && item.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
            .Select(item => item.GetProperty("id").GetString() ?? string.Empty)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (models.Length == 0)
            throw new InvalidOperationException(F("Nu s-au găsit modele de conversație disponibile pentru {0}.", AiProviderIds.DeepSeek));
        return models;
    }

    public async Task<string> GenerateAsync(string key, string model, string instructions, string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException(ProviderText("Introdu cheia API OpenAI în Setări Inteligență artificială."));
        if (string.IsNullOrWhiteSpace(model)) throw new InvalidOperationException(ProviderText("Selectează un model OpenAI în Setări Inteligență artificială."));

        var requestBody = new
        {
            model = model.Trim(),
            messages = new[]
            {
                new { role = "system", content = instructions },
                new { role = "user", content = prompt }
            },
            max_tokens = 2048,
            stream = false
        };
        using var request = CreateRequest(HttpMethod.Post, $"{ApiRoot}/chat/completions", key);
        request.Content = JsonContent.Create(requestBody);
        using var response = await _client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(DescribeError(response.StatusCode, body, key));

        using var document = JsonDocument.Parse(body);
        if (document.RootElement.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0 &&
            choices[0].TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content))
        {
            if (content.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(content.GetString())) return content.GetString()!;
            if (content.ValueKind == JsonValueKind.Array)
            {
                var result = string.Join(Environment.NewLine, content.EnumerateArray()
                    .Where(item => item.TryGetProperty("type", out var type) && type.GetString() == "text" && item.TryGetProperty("text", out _))
                    .Select(item => item.GetProperty("text").GetString())
                    .Where(text => !string.IsNullOrWhiteSpace(text)));
                if (!string.IsNullOrWhiteSpace(result)) return result;
            }
        }

        throw new InvalidOperationException(ProviderText("OpenAI nu a returnat text."));
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string uri, string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException(ProviderText("Introdu mai întâi cheia API OpenAI."));
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key.Trim());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static string DescribeError(HttpStatusCode status, string body, string key)
    {
        var safeBody = string.IsNullOrEmpty(key) ? body : body.Replace(key, "[redacted]", StringComparison.Ordinal);
        if (safeBody.Length > MaximumErrorCharacters) safeBody = safeBody[..MaximumErrorCharacters];
        if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return ProviderFormat("Cheia API OpenAI nu a fost acceptată. Verifică cheia și permisiunile. Detalii: {0}", safeBody);
        if ((int)status == 429)
            return ProviderFormat("OpenAI a limitat solicitarea sau a fost atinsă cota contului. Detalii: {0}", safeBody);
        return ProviderFormat("OpenAI a răspuns cu eroarea {0}. Detalii: {1}", (int)status, safeBody);
    }

    private static string ProviderText(string source) => UiText.Translate(source).Replace("OpenAI", AiProviderIds.DeepSeek, StringComparison.Ordinal);
    private static string ProviderFormat(string source, params object?[] arguments) => F(source, arguments).Replace("OpenAI", AiProviderIds.DeepSeek, StringComparison.Ordinal);
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);
}
