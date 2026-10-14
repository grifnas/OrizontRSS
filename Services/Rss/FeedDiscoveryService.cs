using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

/// <summary>
/// Găsește feedurile unei adrese și le verifică automat: adresa poate fi deja un flux, pagina
/// principală a unui site, un articol, un canal YouTube, un subreddit, un proiect GitHub sau un
/// profil Mastodon. În rezultat apar numai feedurile care au putut fi citite.
/// </summary>
public sealed class FeedDiscoveryService
{
    public const int MaximumParallelChecks = 6;
    private const int MaximumLinkCandidates = 40;
    private static readonly string[] CommonPaths = ["/feed", "/rss", "/rss.xml", "/feed.xml", "/atom.xml", "/index.xml", "/feeds/posts/default", "/?feed=rss2"];
    private static readonly Regex FeedLikeAddress = new(@"(?i)(^|[/.\-_?=])(rss|atom|feeds?)([/.\-_?=]|\.xml|$)|\.rss$|feedburner\.com", RegexOptions.Compiled);
    private static readonly Regex FeedLikeLinkText = new(@"(?i)\b(rss|atom|feeds?|fluxuri)\b", RegexOptions.Compiled);
    private static readonly Regex CommentsFeed = new(@"(?i)comment|comentari|kommentar|commentaire|coment[aá]rio|hozzászól|comment[io]", RegexOptions.Compiled);

    private readonly HttpClient _client;
    private readonly RssReader _reader;

    public FeedDiscoveryService(HttpMessageHandler? handler = null)
    {
        handler ??= CreateHandler();
        _client = new HttpClient(handler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(15) };
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) OrizontRSS/1.6");
        _client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ro,en;q=0.8");
        _reader = new RssReader(handler);
    }

    private static HttpClientHandler CreateHandler()
    {
        var cookies = new CookieContainer();
        // YouTube trimite din Europa la pagina de consimțământ; răspunsul salvat este refuzul cookie-urilor neesențiale.
        cookies.Add(new Cookie("SOCS", "CAI", "/", ".youtube.com"));
        return new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            AllowAutoRedirect = true,
            CheckCertificateRevocationList = true,
            UseCookies = true,
            CookieContainer = cookies
        };
    }

    public static Uri NormalizeAddress(string input)
    {
        var address = (input ?? string.Empty).Trim();
        if (!address.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !address.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            address = "https://" + address;
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) || !uri.Host.Contains('.'))
            throw new InvalidOperationException(UiText.Translate("Introdu o adresă de site validă."));
        return uri;
    }

    public async Task<List<DiscoveredFeed>> DiscoverAsync(string input, CancellationToken cancellationToken = default)
    {
        var start = NormalizeAddress(input);
        var candidates = new List<Candidate>();
        var pageError = await CollectAsync(start, candidates, depth: 0, cancellationToken);
        var (found, unreadable) = await VerifyAsync(candidates, cancellationToken);

        if (found.Count == 0)
        {
            // Paginile-listă de fluxuri ale site-ului (de exemplu „/rss”) sunt citite și ele, o singură dată.
            foreach (var directory in unreadable.Where(candidate => candidate.Rank == 1).Take(4))
            {
                var inner = new List<Candidate>();
                await CollectAsync(new Uri(directory.Url), inner, depth: 1, cancellationToken);
                found.AddRange((await VerifyAsync(inner.Select(item => item with { Rank = 2 }).Take(30).ToList(), cancellationToken)).Found);
            }
        }

        if (found.Count == 0)
        {
            var origin = start.GetLeftPart(UriPartial.Authority);
            found.AddRange((await VerifyAsync(CommonPaths.Select(path => new Candidate(origin + path, 3)).ToList(), cancellationToken)).Found);
        }

        if (found.Count == 0 && pageError is not null) throw pageError;
        return Order(found);
    }

    /// <summary>Feedurile principale primele, cele de comentarii la final; dublurile (aceeași adresă sau același conținut) sunt eliminate.</summary>
    private static List<DiscoveredFeed> Order(List<(DiscoveredFeed Feed, int Rank, string FirstArticle)> found)
    {
        var seenAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenContent = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<DiscoveredFeed>();
        foreach (var item in found.OrderBy(item => item.Feed.IsComments).ThenBy(item => item.Rank))
        {
            var addressKey = AddressKey(item.Feed.Url);
            var contentKey = $"{item.Feed.Name}\n{item.FirstArticle}";
            if (!seenAddresses.Add(addressKey) || (item.FirstArticle.Length > 0 && !seenContent.Add(contentKey))) continue;
            result.Add(item.Feed);
        }
        return result;
    }

    private static string AddressKey(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return url;
        var host = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : uri.Host;
        return $"{host}{uri.AbsolutePath.TrimEnd('/')}{uri.Query}";
    }

    private async Task<Exception?> CollectAsync(Uri page, List<Candidate> candidates, int depth, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await _client.GetAsync(page, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return exception;
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode) return new HttpRequestException(UiText.Format("Serverul a răspuns cu eroarea {0}.", (int)response.StatusCode));
            var final = response.RequestMessage?.RequestUri ?? page;
            var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (LooksLikeFeed(mediaType, body))
            {
                candidates.Add(new Candidate(final.ToString(), 0));
                return null;
            }

            var document = new HtmlParser().ParseDocument(body);
            var baseHref = document.QuerySelector("base[href]")?.GetAttribute("href");
            var resolveFrom = baseHref is not null && Uri.TryCreate(final, baseHref, out var declaredBase) ? declaredBase : final;
            void Add(string? href, int rank)
            {
                if (string.IsNullOrWhiteSpace(href) || !Uri.TryCreate(resolveFrom, href.Trim(), out var address)) return;
                if (address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps) return;
                candidates.Add(new Candidate(address.ToString(), rank));
            }

            foreach (var link in document.QuerySelectorAll("link[href]"))
            {
                var rel = (link.GetAttribute("rel") ?? string.Empty).ToLowerInvariant();
                var type = (link.GetAttribute("type") ?? string.Empty).ToLowerInvariant();
                if (rel.Contains("alternate") && (type.Contains("rss") || type.Contains("atom") || type.Contains("rdf") || type is "application/xml" or "text/xml"))
                    Add(link.GetAttribute("href"), 0);
            }

            var linkCandidates = 0;
            foreach (var anchor in document.QuerySelectorAll("a[href]"))
            {
                if (linkCandidates >= MaximumLinkCandidates) break;
                var href = anchor.GetAttribute("href") ?? string.Empty;
                var text = anchor.TextContent.Trim();
                if (FeedLikeAddress.IsMatch(href) || (text.Length < 40 && FeedLikeLinkText.IsMatch(text)))
                {
                    Add(href, 1);
                    linkCandidates++;
                }
            }

            AddPlatformCandidates(final, body, document.QuerySelector("meta[name=generator]")?.GetAttribute("content") ?? string.Empty, candidates);

            // Adresa unui articol: se caută și pe pagina principală a site-ului.
            if (depth == 0 && final.AbsolutePath.Trim('/').Length > 0)
                await CollectAsync(new Uri(final.GetLeftPart(UriPartial.Authority) + "/"), candidates, depth: 1, cancellationToken);
            return null;
        }
    }

    private static void AddPlatformCandidates(Uri page, string html, string generator, List<Candidate> candidates)
    {
        var host = page.Host.ToLowerInvariant();
        if (host.EndsWith("youtube.com", StringComparison.Ordinal))
        {
            var channelId = Regex.Match(html, @"""(?:channelId|externalId)""\s*:\s*""(UC[\w-]{22})""").Groups[1].Value;
            if (channelId.Length == 0) channelId = Regex.Match(page.AbsolutePath, @"^/channel/(UC[\w-]{22})").Groups[1].Value;
            if (channelId.Length > 0) candidates.Add(new Candidate($"https://www.youtube.com/feeds/videos.xml?channel_id={channelId}", 0));
        }
        if (host.EndsWith("reddit.com", StringComparison.Ordinal) && Regex.Match(page.AbsolutePath, @"^/(r|user)/[^/]+") is { Success: true } reddit)
            candidates.Add(new Candidate($"https://www.reddit.com{reddit.Value}/.rss", 0));
        if (host == "github.com" && Regex.Match(page.AbsolutePath, @"^/[^/]+/[^/]+") is { Success: true } repository)
        {
            candidates.Add(new Candidate($"https://github.com{repository.Value}/releases.atom", 0));
            candidates.Add(new Candidate($"https://github.com{repository.Value}/commits.atom", 0));
        }
        // Profiluri Mastodon și alte servere Fediverse: https://server/@nume are fluxul https://server/@nume.rss.
        if (Regex.IsMatch(page.AbsolutePath, @"^/@[\w.]+/?$") && !host.EndsWith("youtube.com", StringComparison.Ordinal) && !host.EndsWith("medium.com", StringComparison.Ordinal))
            candidates.Add(new Candidate(page.GetLeftPart(UriPartial.Path).TrimEnd('/') + ".rss", 0));
        if (host.EndsWith("medium.com", StringComparison.Ordinal) && Regex.Match(page.AbsolutePath, @"^/@[^/]+") is { Success: true } medium)
            candidates.Add(new Candidate($"https://medium.com/feed{medium.Value}", 0));
        if (generator.Contains("WordPress", StringComparison.OrdinalIgnoreCase)) candidates.Add(new Candidate(new Uri(page, "/feed/").ToString(), 0));
        if (generator.Contains("Blogger", StringComparison.OrdinalIgnoreCase)) candidates.Add(new Candidate(new Uri(page, "/feeds/posts/default").ToString(), 0));
        if (generator.Contains("Ghost", StringComparison.OrdinalIgnoreCase)) candidates.Add(new Candidate(new Uri(page, "/rss/").ToString(), 0));
        if (html.Contains("substackcdn.com", StringComparison.Ordinal)) candidates.Add(new Candidate(new Uri(page, "/feed").ToString(), 0));
    }

    private static bool LooksLikeFeed(string mediaType, string body)
    {
        if (mediaType.Contains("rss", StringComparison.OrdinalIgnoreCase) || mediaType.Contains("atom", StringComparison.OrdinalIgnoreCase)) return true;
        var start = body.TrimStart();
        if (start.Length > 400) start = start[..400];
        return Regex.IsMatch(start, @"^(<\?xml[^>]*>\s*)?(<!--[\s\S]*?-->\s*)*(<\?xml-stylesheet[^>]*>\s*)*<(rss|feed|rdf:RDF)\b");
    }

    private async Task<(List<(DiscoveredFeed Feed, int Rank, string FirstArticle)> Found, List<Candidate> Unreadable)> VerifyAsync(List<Candidate> candidates, CancellationToken cancellationToken)
    {
        var distinct = candidates.GroupBy(candidate => candidate.Url, StringComparer.OrdinalIgnoreCase).Select(group => group.OrderBy(candidate => candidate.Rank).First()).ToList();
        var gate = new SemaphoreSlim(MaximumParallelChecks);
        var checks = await Task.WhenAll(distinct.Select(async candidate =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var preview = await _reader.LoadPreviewAsync(candidate.Url, cancellationToken).ConfigureAwait(false);
                return (candidate, preview: (RssFeedPreview?)preview);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                return (candidate, preview: (RssFeedPreview?)null);
            }
            finally
            {
                gate.Release();
            }
        })).ConfigureAwait(false);

        var found = new List<(DiscoveredFeed, int, string)>();
        var unreadable = new List<Candidate>();
        foreach (var (candidate, preview) in checks)
        {
            if (preview is null || preview.Articles.Count == 0) { unreadable.Add(candidate); continue; }
            var host = Uri.TryCreate(preview.Url, UriKind.Absolute, out var uri) ? uri.Host : string.Empty;
            var name = string.IsNullOrWhiteSpace(preview.Title) ? host : preview.Title;
            found.Add((new DiscoveredFeed
            {
                Name = name,
                Url = preview.Url,
                SourceSite = host,
                IsVerified = true,
                ArticleCount = preview.Articles.Count,
                LatestArticle = preview.Articles.Max(article => article.Published),
                IsComments = CommentsFeed.IsMatch(preview.Url) || CommentsFeed.IsMatch(name)
            }, candidate.Rank, preview.Articles[0].Id));
        }
        return (found, unreadable);
    }

    private sealed record Candidate(string Url, int Rank);
}
