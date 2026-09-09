using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Xml.Linq;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public sealed record NewsBlurSession(string Username, string SessionId);
public sealed record NewsBlurSubscription(string Name, string Url, string Folder);

/// <summary>Cookie-based NewsBlur authentication and read-only subscription import.</summary>
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

    public async Task<int?> GetFeedCountAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) throw new InvalidOperationException(T("Sesiunea NewsBlur nu este disponibilă."));
        using var handler = CreateHandler();
        handler.CookieContainer.Add(new Uri(ApiBaseUrl), new Cookie("newsblur_sessionid", sessionId));
        using var client = CreateClient(handler);
        using var response = await client.GetAsync("/reader/feeds?flat=true", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response.StatusCode, body, T("Sesiunea NewsBlur nu mai este valabilă."));
        try
        {
            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("feeds", out var feeds)) return null;
            return feeds.ValueKind switch
            {
                JsonValueKind.Object => feeds.EnumerateObject().Count(),
                JsonValueKind.Array => feeds.GetArrayLength(),
                _ => null
            };
        }
        catch (JsonException) { return null; }
    }

    public async Task<IReadOnlyList<NewsBlurSubscription>> GetSubscriptionsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) throw new InvalidOperationException(T("Sesiunea NewsBlur nu este disponibilă."));
        using var handler = CreateHandler();
        handler.CookieContainer.Add(new Uri(ApiBaseUrl), new Cookie("newsblur_sessionid", sessionId));
        using var client = CreateClient(handler);
        using var response = await client.GetAsync("/import/opml_export", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response.StatusCode, body, T("Sincronizarea abonamentelor NewsBlur a eșuat."));
        XDocument document;
        try { document = XDocument.Parse(body); }
        catch (Exception exception) when (exception is System.Xml.XmlException or InvalidOperationException)
        {
            throw new InvalidOperationException(T("NewsBlur nu a returnat un fișier OPML valid."), exception);
        }

        var subscriptions = new List<NewsBlurSubscription>();
        foreach (var outline in document.Descendants().Where(element => element.Name.LocalName == "outline" && element.Parent?.Name.LocalName != "outline"))
            CollectSubscriptions(outline, null, subscriptions);
        return subscriptions
            .Where(subscription => !string.IsNullOrWhiteSpace(subscription.Url))
            .GroupBy(subscription => DuplicateCleaner.FeedKey(subscription.Url), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static void CollectSubscriptions(XElement outline, string? parentFolder, ICollection<NewsBlurSubscription> subscriptions)
    {
        var address = outline.Attribute("xmlUrl")?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(address) && Uri.TryCreate(address, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
        {
            var name = outline.Attribute("text")?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(name)) name = outline.Attribute("title")?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(name)) name = address;
            subscriptions.Add(new NewsBlurSubscription(name, address, string.IsNullOrWhiteSpace(parentFolder) ? "Neorganizate" : parentFolder));
            return;
        }

        var folderName = outline.Attribute("text")?.Value?.Trim() ?? outline.Attribute("title")?.Value?.Trim();
        var folder = string.IsNullOrWhiteSpace(folderName)
            ? parentFolder
            : string.IsNullOrWhiteSpace(parentFolder) ? folderName : $"{parentFolder} / {folderName}";
        foreach (var child in outline.Elements().Where(element => element.Name.LocalName == "outline"))
            CollectSubscriptions(child, folder, subscriptions);
    }

    private static HttpClientHandler CreateHandler() => new()
    {
        UseCookies = true,
        CookieContainer = new CookieContainer(),
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        AllowAutoRedirect = true
    };

    private static HttpClient CreateClient(HttpClientHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(20)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("OrizontRSS/1.5.4");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        return client;
    }

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
            if (!document.RootElement.TryGetProperty("errors", out var errors)) return false;
            return errors.ValueKind switch
            {
                JsonValueKind.Array => errors.GetArrayLength() > 0,
                JsonValueKind.Object => errors.EnumerateObject().Any(),
                JsonValueKind.String => !string.IsNullOrWhiteSpace(errors.GetString()),
                _ => false
            };
        }
        catch (JsonException) { return false; }
    }

    private static string DescribeFailure(string body, string fallback)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("errors", out var errors))
            {
                var messages = ErrorMessages(errors);
                var text = string.Join("; ", messages);
                if (!string.IsNullOrWhiteSpace(text)) return $"{fallback} {text}";
            }
            if (document.RootElement.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(message.GetString()))
                return $"{fallback} {message.GetString()}";
        }
        catch (JsonException) { }
        return fallback;
    }

    private static IEnumerable<string> ErrorMessages(JsonElement errors)
    {
        return errors.ValueKind switch
        {
            JsonValueKind.Array => errors.EnumerateArray().SelectMany(ErrorMessages),
            JsonValueKind.Object => errors.EnumerateObject().SelectMany(property => ErrorMessages(property.Value).Select(message => $"{property.Name}: {message}")),
            JsonValueKind.String when !string.IsNullOrWhiteSpace(errors.GetString()) => [errors.GetString()!],
            JsonValueKind.Null or JsonValueKind.Undefined => [],
            _ => [errors.ToString()]
        };
    }

    private static void EnsureSuccess(HttpStatusCode status, string body, string fallback)
    {
        if ((int)status is 401 or 403) throw new InvalidOperationException(DescribeFailure(body, T("NewsBlur a refuzat autentificarea. Verifică datele introduse.")));
        if (!((int)status >= 200 && (int)status <= 299)) throw new InvalidOperationException($"{fallback} HTTP {(int)status}.");
    }

    private static string T(string source) => UiText.Translate(source);
}
