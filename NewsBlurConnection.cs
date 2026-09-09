using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public sealed record NewsBlurSession(string Username, string SessionId);
public sealed record NewsBlurSubscription(string Name, string Url, string Folder);
public sealed record NewsBlurFeedInfo(string FeedId, string Name, string Url, string Folder);
public sealed record NewsBlurStory(string StoryHash, string Title, string Link, DateTimeOffset? Published, bool IsRead, bool IsStarred, IReadOnlyList<string> UserTags, string Content);

/// <summary>Cookie-based NewsBlur authentication and controlled subscription/story synchronization.</summary>
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

    public async Task<IReadOnlyList<NewsBlurFeedInfo>> GetFeedIndexAsync(string sessionId, CancellationToken cancellationToken = default)
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
            if (!document.RootElement.TryGetProperty("feeds", out var feeds) || feeds.ValueKind != JsonValueKind.Object) return [];
            var folders = ReadFolderMap(document.RootElement);
            return feeds.EnumerateObject()
                .Select(property =>
                {
                    var value = property.Value;
                    var url = ReadString(value, "feed_address", "feed_url", "feed_link");
                    var name = ReadString(value, "feed_title", "title") ?? url ?? property.Name;
                    var folder = ReadString(value, "folder") ?? (folders.TryGetValue(property.Name, out var mapped) ? mapped : "Neorganizate");
                    return new NewsBlurFeedInfo(property.Name, name, url ?? string.Empty, folder);
                })
                .Where(feed => Uri.TryCreate(feed.Url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
                .ToList();
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(T("NewsBlur nu a returnat un răspuns valid pentru feeduri."), exception);
        }
    }

    public async Task<IReadOnlyList<NewsBlurStory>> GetStoriesAsync(string sessionId, string feedId, DateTimeOffset? oldestLocalArticle = null, int maxPages = 40, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) throw new InvalidOperationException(T("Sesiunea NewsBlur nu este disponibilă."));
        if (string.IsNullOrWhiteSpace(feedId)) return [];
        using var handler = CreateHandler();
        handler.CookieContainer.Add(new Uri(ApiBaseUrl), new Cookie("newsblur_sessionid", sessionId));
        using var client = CreateClient(handler);
        var stories = new List<NewsBlurStory>();
        for (var page = 1; page <= Math.Max(1, maxPages); page++)
        {
            if (page > 1) await Task.Delay(80, cancellationToken);
            var address = $"/reader/feed/{Uri.EscapeDataString(feedId)}?page={page}&order=newest&read_filter=all&include_hidden=false&include_story_content=true";
            using var response = await client.GetAsync(address, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            EnsureSuccess(response.StatusCode, body, T("Sincronizarea articolelor NewsBlur a eșuat."));
            IReadOnlyList<NewsBlurStory> pageStories;
            try { pageStories = ParseStories(body); }
            catch (JsonException exception) { throw new InvalidOperationException(T("NewsBlur nu a returnat articole într-un format valid."), exception); }
            if (pageStories.Count == 0) break;
            stories.AddRange(pageStories);
            if (oldestLocalArticle.HasValue && pageStories.Min(story => story.Published ?? DateTimeOffset.MaxValue) < oldestLocalArticle.Value) break;
        }
        return stories
            .Where(story => !string.IsNullOrWhiteSpace(story.StoryHash))
            .GroupBy(story => story.StoryHash, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    /// <summary>Adds a feed to the authenticated NewsBlur account.</summary>
    public async Task AddFeedAsync(string sessionId, string url, string? folder = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException(T("Adresa feedului este obligatorie."), nameof(url));
        var values = new List<KeyValuePair<string, string>> { new("url", url.Trim()) };
        if (!string.IsNullOrWhiteSpace(folder) && !string.Equals(folder.Trim(), "Neorganizate", StringComparison.CurrentCultureIgnoreCase))
            values.Add(new("folder", folder.Trim()));
        await PostAccountChangeAsync(sessionId, "/reader/add_url", values, T("Feedul nu a putut fi adăugat în NewsBlur."), cancellationToken);
    }

    /// <summary>Creates a NewsBlur folder. Empty folders have no local equivalent, but nested folders are supported.</summary>
    public async Task AddFolderAsync(string sessionId, string folder, string? parentFolder = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folder)) return;
        var values = new List<KeyValuePair<string, string>> { new("folder", folder.Trim()) };
        if (!string.IsNullOrWhiteSpace(parentFolder)) values.Add(new("parent_folder", parentFolder.Trim()));
        await PostAccountChangeAsync(sessionId, "/reader/add_folder", values, T("Folderul nu a putut fi creat în NewsBlur."), cancellationToken);
    }

    public async Task MarkStoriesReadAsync(string sessionId, IEnumerable<string> storyHashes, bool isRead, CancellationToken cancellationToken = default)
    {
        var hashes = storyHashes.Where(hash => !string.IsNullOrWhiteSpace(hash)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (hashes.Count == 0) return;
        var endpoint = isRead ? "/reader/mark_story_hashes_as_read" : "/reader/mark_story_hash_as_unread";
        await PostStoryHashesAsync(sessionId, endpoint, hashes, [], cancellationToken);
    }

    public async Task MarkStoryStarredAsync(string sessionId, string storyHash, bool isStarred, IEnumerable<string>? userTags = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storyHash)) return;
        var endpoint = isStarred ? "/reader/mark_story_hash_as_starred" : "/reader/mark_story_hash_as_unstarred";
        var tags = isStarred ? (userTags ?? []).ToList() : [];
        await PostStoryHashesAsync(sessionId, endpoint, [storyHash], tags, cancellationToken);
    }

    private async Task PostStoryHashesAsync(string sessionId, string endpoint, IReadOnlyList<string> hashes, IReadOnlyList<string> userTags, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) throw new InvalidOperationException(T("Sesiunea NewsBlur nu este disponibilă."));
        using var handler = CreateHandler();
        handler.CookieContainer.Add(new Uri(ApiBaseUrl), new Cookie("newsblur_sessionid", sessionId));
        using var client = CreateClient(handler);
        foreach (var batch in hashes.Chunk(50))
        {
            var values = batch.Select(hash => new KeyValuePair<string, string>("story_hash", hash)).ToList();
            if (endpoint.EndsWith("starred", StringComparison.Ordinal) && userTags.Count > 0)
                values.AddRange(userTags.Select(tag => new KeyValuePair<string, string>("user_tags[]", tag)));
            using var response = await client.PostAsync(endpoint, new FormUrlEncodedContent(values), cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            EnsureSuccess(response.StatusCode, body, T("Sincronizarea stărilor NewsBlur a eșuat."));
            if (batch.Length < hashes.Count) await Task.Delay(80, cancellationToken);
        }
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

    private static Dictionary<string, string> ReadFolderMap(JsonElement root)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!root.TryGetProperty("folders", out var folders) || folders.ValueKind != JsonValueKind.Object) return result;
        foreach (var folder in folders.EnumerateObject())
        {
            if (folder.Value.ValueKind == JsonValueKind.Array)
                foreach (var feedId in folder.Value.EnumerateArray())
                    if (feedId.ValueKind == JsonValueKind.Number || feedId.ValueKind == JsonValueKind.String)
                        result[feedId.ToString()] = folder.Name;
        }
        return result;
    }

    private static IReadOnlyList<NewsBlurStory> ParseStories(string body)
    {
        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("stories", out var stories) || stories.ValueKind != JsonValueKind.Array) return [];
        return stories.EnumerateArray().Select(story =>
        {
            var hash = ReadString(story, "story_hash") ?? string.Empty;
            var title = ReadString(story, "story_title", "title") ?? string.Empty;
            var link = ReadString(story, "story_permalink", "story_link", "story_guid", "guid") ?? string.Empty;
            var published = ReadDate(story, "story_timestamp", "story_date", "published");
            var read = ReadBoolean(story, "read_status", "read");
            var starred = ReadBoolean(story, "starred", "is_starred");
            var tags = ReadTags(story);
            var content = CleanStoryContent(ReadString(story, "story_content", "content") ?? string.Empty);
            return new NewsBlurStory(hash, title, link, published, read, starred, tags, content);
        }).ToList();
    }

    private async Task PostAccountChangeAsync(string sessionId, string endpoint, IEnumerable<KeyValuePair<string, string>> values, string fallback, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) throw new InvalidOperationException(T("Sesiunea NewsBlur nu este disponibilă."));
        using var handler = CreateHandler();
        handler.CookieContainer.Add(new Uri(ApiBaseUrl), new Cookie("newsblur_sessionid", sessionId));
        using var client = CreateClient(handler);
        using var response = await client.PostAsync(endpoint, new FormUrlEncodedContent(values), cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response.StatusCode, body, fallback);
        if (HasErrors(body)) throw new InvalidOperationException(DescribeFailure(body, fallback));
        await Task.Delay(80, cancellationToken);
    }

    private static string CleanStoryContent(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var withoutTags = Regex.Replace(value, "<[^>]+>", " ", RegexOptions.Singleline);
        return WebUtility.HtmlDecode(Regex.Replace(withoutTags, "\\s+", " ")).Trim();
    }

    private static string? ReadString(JsonElement value, params string[] names)
    {
        foreach (var name in names)
            if (value.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(property.GetString()))
                return property.GetString()!.Trim();
        return null;
    }

    private static bool ReadBoolean(JsonElement value, params string[] names)
    {
        foreach (var name in names)
        {
            if (!value.TryGetProperty(name, out var property)) continue;
            if (property.ValueKind == JsonValueKind.True) return true;
            if (property.ValueKind == JsonValueKind.False) return false;
            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number)) return number != 0;
            if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out number)) return number != 0;
        }
        return false;
    }

    private static DateTimeOffset? ReadDate(JsonElement value, params string[] names)
    {
        foreach (var name in names)
        {
            if (!value.TryGetProperty(name, out var property)) continue;
            if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var seconds))
                return DateTimeOffset.UnixEpoch.AddSeconds(seconds).ToLocalTime();
            if (property.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(property.GetString(), out var parsed)) return parsed;
        }
        return null;
    }

    private static IReadOnlyList<string> ReadTags(JsonElement value)
    {
        if (!value.TryGetProperty("user_tags", out var tags)) return [];
        if (tags.ValueKind == JsonValueKind.Array)
            return tags.EnumerateArray().Where(tag => tag.ValueKind == JsonValueKind.String).Select(tag => tag.GetString()!.Trim()).Where(tag => tag.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (tags.ValueKind == JsonValueKind.Object)
            return tags.EnumerateObject().Select(property => property.Name.Trim()).Where(tag => tag.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return [];
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
