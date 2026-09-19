using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public sealed class GeminiConnection
{
    public async Task TestAsync(string key)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        client.DefaultRequestHeaders.Add("x-goog-api-key", key.Trim());
        using var response = await client.GetAsync("https://generativelanguage.googleapis.com/v1beta/models?pageSize=1");
        if (response.IsSuccessStatusCode) return;

        var detail = await response.Content.ReadAsStringAsync();
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new InvalidOperationException(T("Cheia API Gemini nu a fost acceptată. Creează sau verifică cheia în Google AI Studio."));
        if ((int)response.StatusCode == 429)
            throw new InvalidOperationException(T("Gemini a limitat temporar solicitarea sau a fost atinsă cota contului."));
        throw new InvalidOperationException(F("Gemini a răspuns cu eroarea {0}. {1}", (int)response.StatusCode, ShortDetail(detail)));
    }

    public async Task<string> GenerateAsync(string key, string systemInstruction, string prompt)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        client.DefaultRequestHeaders.Add("x-goog-api-key", key.Trim());
        var model = await GetTextModelAsync(client);
        var request = new
        {
            systemInstruction = new { parts = new[] { new { text = systemInstruction } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
            generationConfig = new { temperature = 0.3, maxOutputTokens = 2048 }
        };
        using var response = await client.PostAsJsonAsync($"https://generativelanguage.googleapis.com/v1beta/{model}:generateContent", request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(DescribeRequestError(response.StatusCode, body));

        using var document = JsonDocument.Parse(body);
        if (document.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0 &&
            candidates[0].TryGetProperty("content", out var content) && content.TryGetProperty("parts", out var parts))
        {
            var text = string.Join(Environment.NewLine, parts.EnumerateArray()
                .Where(part => part.TryGetProperty("text", out _))
                .Select(part => part.GetProperty("text").GetString()));
            if (!string.IsNullOrWhiteSpace(text)) return text;
        }
        throw new InvalidOperationException(T("Gemini nu a returnat text. Este posibil ca răspunsul să fi fost filtrat de regulile de siguranță."));
    }

    private static async Task<string> GetTextModelAsync(HttpClient client)
    {
        using var response = await client.GetAsync("https://generativelanguage.googleapis.com/v1beta/models?pageSize=100");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(F("Nu s-au putut obține modelele Gemini: {0}. {1}", (int)response.StatusCode, ShortDetail(body)));
        using var document = JsonDocument.Parse(body);
        var models = document.RootElement.GetProperty("models").EnumerateArray()
            .Where(item => item.TryGetProperty("supportedGenerationMethods", out var methods) && methods.EnumerateArray().Any(method => method.GetString() == "generateContent"))
            .Select(item => item.GetProperty("name").GetString() ?? string.Empty).ToList();
        var preferred = new[] { "models/gemini-3.5-flash", "models/gemini-2.5-flash", "models/gemini-2.0-flash" };
        return preferred.FirstOrDefault(models.Contains) ?? models.FirstOrDefault() ?? throw new InvalidOperationException(T("Cheia Gemini nu are niciun model disponibil pentru generarea de text."));
    }

    private static string ShortDetail(string detail) => detail.Length > 240 ? detail[..240] : detail;
    private static string DescribeRequestError(HttpStatusCode status, string body)
    {
        var detail = ShortDetail(body);
        if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) return F("Gemini a refuzat comanda. Verifică cheia API, proiectul Google și permisiunile. Detalii server: {0}", detail);
        if ((int)status == 429) return F("Gemini a limitat temporar comanda sau a fost atinsă cota contului. Detalii server: {0}", detail);
        return F("Gemini a răspuns cu eroarea {0}. Detalii server: {1}", (int)status, detail);
    }
    private static string T(string source) => UiText.Translate(source);
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);
}
