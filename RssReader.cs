using System.Net;
using System.Net.Http;
using System.Xml.Linq;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public sealed class RssReader
{
    private readonly HttpClient _client;
    public RssReader()
    {
        _client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All, AllowAutoRedirect = true, CheckCertificateRevocationList = true }) { Timeout = TimeSpan.FromSeconds(30) };
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("CititorRSS-JAWS/1.0");
        _client.DefaultRequestHeaders.Accept.ParseAdd("application/rss+xml, application/atom+xml, application/xml, text/xml, */*");
    }
    public async Task<List<Article>> LoadAsync(string url, CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var xml = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
        var records = xml.Descendants().Where(element => element.Name.LocalName is "item" or "entry").ToList();
        if (records.Count == 0) throw new InvalidOperationException(UiText.Translate("Adresa nu conține un flux RSS sau Atom cu articole."));
        return records.Select(record =>
        {
            string Field(string localName) => record.Elements().FirstOrDefault(element => element.Name.LocalName == localName)?.Value ?? string.Empty;
            var html = Field("encoded");
            if (string.IsNullOrWhiteSpace(html)) html = Field("content");
            if (string.IsNullOrWhiteSpace(html)) html = Field("description");
            if (string.IsNullOrWhiteSpace(html)) html = Field("summary");
            var link = record.Elements().FirstOrDefault(element => element.Name.LocalName == "link")?.Attribute("href")?.Value ?? Field("link");
            var date = Field("pubDate");
            if (string.IsNullOrWhiteSpace(date)) date = Field("published");
            if (string.IsNullOrWhiteSpace(date)) date = Field("updated");
            var title = WebUtility.HtmlDecode(Field("title")).Trim();
            var id = Field("guid");
            if (string.IsNullOrWhiteSpace(id)) id = Field("id");
            if (string.IsNullOrWhiteSpace(id)) id = link;
            if (string.IsNullOrWhiteSpace(id)) id = $"{title}\n{date}";
            var text = WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " "));
            text = System.Text.RegularExpressions.Regex.Replace(text, "\\s+", " ").Trim();
            return new Article { Id = id, Title = title, Content = text, Link = link, Published = DateTimeOffset.TryParse(date, out var parsed) ? parsed : DateTimeOffset.Now };
        }).OrderByDescending(item => item.Published).ToList();
    }

    public async Task<string> LoadReadableContentAsync(string articleUrl)
    {
        using var response = await _client.GetAsync(articleUrl);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        return await Task.Run(() => ExtractReadableText(html));
    }

    public async Task<List<DiscoveredFeed>> DiscoverAsync(string siteUrl)
    {
        siteUrl = siteUrl.Trim();
        if (!siteUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !siteUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) siteUrl = "https://" + siteUrl;
        if (!Uri.TryCreate(siteUrl, UriKind.Absolute, out var site) || (site.Scheme != Uri.UriSchemeHttp && site.Scheme != Uri.UriSchemeHttps)) throw new InvalidOperationException(UiText.Translate("Introdu o adresă de site validă."));
        var results = new Dictionary<string, DiscoveredFeed>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var html = await _client.GetStringAsync(site);
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(html, "<link\\b[^>]*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                var tag = match.Value;
                var href = Attribute(tag, "href");
                var type = Attribute(tag, "type");
                if (string.IsNullOrWhiteSpace(href) || !Uri.TryCreate(site, href, out var feedAddress)) continue;
                if (!type.Contains("rss", StringComparison.OrdinalIgnoreCase) && !type.Contains("atom", StringComparison.OrdinalIgnoreCase)) continue;
                var label = Attribute(tag, "title");
                results.TryAdd(feedAddress.ToString(), new DiscoveredFeed { Name = string.IsNullOrWhiteSpace(label) ? $"{site.Host} — feed declarat" : label, Url = feedAddress.ToString(), SourceSite = site.Host });
            }
        }
        catch { }
        foreach (var path in new[] { "/feed", "/feed/", "/rss", "/rss/", "/rss.xml", "/feed.xml", "/atom.xml", "/index.xml" })
        {
            var address = new Uri(site, path).ToString();
            if (!results.ContainsKey(address)) results[address] = new DiscoveredFeed { Name = $"{site.Host} — posibil feed", Url = address, SourceSite = site.Host, RequiresVerification = true };
        }
        return results.Values.ToList();
    }

    private static string Attribute(string tag, string name)
    {
        var match = System.Text.RegularExpressions.Regex.Match(tag, $"{name}\\s*=\\s*[\\\"'](?<value>[^\\\"']+)[\\\"']", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Groups["value"].Value;
    }
    private static string ExtractHtmlElement(string html, string name) => System.Text.RegularExpressions.Regex.Match(html, $"<{name}\\b[^>]*>([\\s\\S]*?)</{name}>", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Groups[1].Value;
    private static string ExtractReadableText(string html)
    {
        var article = ExtractHtmlElement(html, "article");
        if (string.IsNullOrWhiteSpace(article)) article = ExtractHtmlElement(html, "main");
        if (string.IsNullOrWhiteSpace(article)) article = html;
        article = System.Text.RegularExpressions.Regex.Replace(article, @"\\*<(script|style|noscript|svg|nav|header|footer|aside)\b[^>]*>[\s\S]*?\\*</\1\s*>", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        article = System.Text.RegularExpressions.Regex.Replace(article, @"\\*<text\b[\s\S]*?\\*</svg>", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        article = System.Text.RegularExpressions.Regex.Replace(article, "background-(?:position|repeat|image|size|color)\\s*:[^;]+;?", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        article = System.Text.RegularExpressions.Regex.Replace(article, @"\\+(?=[<>])", string.Empty);
        article = System.Text.RegularExpressions.Regex.Replace(article, "<!--[\\s\\S]*?-->", " ");
        article = System.Text.RegularExpressions.Regex.Replace(article, "<(br|/p|/div|/li|/h[1-6])[^>]*>", Environment.NewLine, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var text = WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(article, "<[^>]+>", " "));
        text = WebUtility.HtmlDecode(text);
        // Reclamele livrate ca JavaScript escapate pot rămâne text după eliminarea tagurilor.
        text = System.Text.RegularExpressions.Regex.Replace(text, @"(?is)googletag\.cmd\.push\(function\(\).*?(?:\}\);|$)", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"(?is)<text\b.*?</svg>", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s*""\);\s*""\s*>\s*", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"(?im)^\s*publicitate\s*$", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"(?im)^\s*\d+\s*/\s*\d+\s*$", " ");
        // Subsolurile editoriale și meniurile de politică nu fac parte din articol.
        text = System.Text.RegularExpressions.Regex.Replace(text, @"(?is)\bO campanie editorială\b.*$", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, "[ \\t]+", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, "(\\r?\\n\\s*){2,}", Environment.NewLine + Environment.NewLine);
        return text.Trim();
    }
}
