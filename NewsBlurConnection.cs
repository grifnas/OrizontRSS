using System.Net;
using System.Net.Http;
using System.Text.Json;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public sealed record NewsBlurSession(string Username, string SessionId);

/// <summary>Minimal, cookie-based NewsBlur authentication for the first sync phase.</summary>
public sealed class NewsBlurConnection
{
    public const string ApiBaseUrl = "https://www.newsblur.com";

    public async Task<NewsBlurSession> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException(T("Numele de utilizator NewsBlur este obligatoriu."), nameof(username));
        if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException(T("Parola NewsBlur este obligatorie."), nameof(password));
        using var handler = CreateHandler();
        using var client = CreateClient(handler);
        using var response = await client.PostAsync("/api/login", Form(username, password), cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response.StatusCode, body, T("Autentificarea NewsBlur a eșuat."));
        if (!IsAuthenticated(body)) throw new InvalidOperationException(DescribeFailure(body, T("Autentificarea NewsBlur a eșuat.")));
        var cookie = handler.CookieContainer.GetCookies(new Uri(ApiBaseUrl))["newsblur_sessionid"]?.Value;
        if (string.IsNullOrWhiteSpace(cookie)) throw new InvalidOperationException(T("NewsBlur nu a returnat sesiunea de autentificare."));
        return new NewsBlurSession(username.Trim(), cookie);
    }

    public async Task SignupAsync(string username, string email, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException(T("Numele de utilizator NewsBlur este obligatoriu."), nameof(username));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException(T("Adresa de e-mail este obligatorie."), nameof(email));
        if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException(T("Parola NewsBlur este obligatorie."), nameof(password));
        using var client = CreateClient(CreateHandler());
        using var response = await client.PostAsync("/api/signup", Form(("username", username.Trim()), ("password", password), ("email", email.Trim())), cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response.StatusCode, body, T("Crearea contului NewsBlur a eșuat."));
        if (HasErrors(body)) throw new InvalidOperationException(DescribeFailure(body, T("Crearea contului NewsBlur a eșuat.")));
    }

    public async Task LogoutAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return;
        using var handler = CreateHandler();
        handler.CookieContainer.Add(new Uri(ApiBaseUrl), new Cookie("newsblur_sessionid", sessionId));
        using var client = CreateClient(handler);
        using var response = await client.PostAsync("/api/logout", new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>()), cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response.StatusCode, body, T("Deconectarea NewsBlur a eșuat."));
    }

    private static HttpClientHandler CreateHandler() => new()
    {
        UseCookies = true,
        CookieContainer = new CookieContainer(),
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        AllowAutoRedirect = true
    };

    private static HttpClient CreateClient(HttpClientHandler handler) => new(handler)
    {
        BaseAddress = new Uri(ApiBaseUrl),
        Timeout = TimeSpan.FromSeconds(20)
    };

    private static FormUrlEncodedContent Form(string username, string password) => Form(("username", username.Trim()), ("password", password));

    private static FormUrlEncodedContent Form(params (string Name, string Value)[] values) =>
        new(values.Select(value => new KeyValuePair<string, string>(value.Name, value.Value)));

    private static bool IsAuthenticated(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty("authenticated", out var authenticated) && authenticated.ValueKind == JsonValueKind.True;
        }
        catch (JsonException) { return false; }
    }

    private static bool HasErrors(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0;
        }
        catch (JsonException) { return false; }
    }

    private static string DescribeFailure(string body, string fallback)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
            {
                var messages = errors.EnumerateArray().Select(error => error.ValueKind == JsonValueKind.String ? error.GetString() : error.ToString()).Where(message => !string.IsNullOrWhiteSpace(message));
                var text = string.Join("; ", messages);
                if (!string.IsNullOrWhiteSpace(text)) return $"{fallback} {text}";
            }
            if (document.RootElement.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(message.GetString()))
                return $"{fallback} {message.GetString()}";
        }
        catch (JsonException) { }
        return fallback;
    }

    private static void EnsureSuccess(HttpStatusCode status, string body, string fallback)
    {
        if ((int)status is 401 or 403) throw new InvalidOperationException(DescribeFailure(body, T("NewsBlur a refuzat autentificarea. Verifică datele introduse.")));
        if (!((int)status >= 200 && (int)status <= 299)) throw new InvalidOperationException($"{fallback} HTTP {(int)status}.");
    }

    private static string T(string source) => UiText.Translate(source);
}
