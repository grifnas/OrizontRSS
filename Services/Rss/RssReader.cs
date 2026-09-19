using System.Net;
using System.Net.Http;
using System.Xml.Linq;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public sealed class RssReader
{
    private static readonly System.Text.RegularExpressions.Regex InlineAdLabels = new(
        @"(?i)\b(?:publicitate|publicidade|advertisement|advertising|werbung|publicidad|publicité|hirdetés|pubblicità)\b",
        System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    private static readonly System.Text.RegularExpressions.Regex CommentLabels = new(
        @"(?i)\b(?:Postați comentariul(?: dvs\.)?|Post a comment|Post your commen\w*|Leave a comment|Kommentar schreiben|Kommentieren|Kommentar|Kommentare|Publicar un comentario|Dejar un comentario|Comentario|Comentarios|Publier un commentaire|Laisser un commentaire|Commentaire|Commentaires|Publicar um comentário|Deixe um comentário|Comentário|Comentários|Hozzászólás írása|Megjegyzés|Megjegyzések|Pubblica un commento|Lascia un commento|Commento|Commenti)\b",
        System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    private static readonly System.Text.RegularExpressions.Regex CommentCounts = new(
        @"(?i)\b(?:Comentarii|Comments|Kommentare|Comentarios|Commentaires|Comentários|Megjegyzések|Commenti)\s*\(\s*\d+\s*\)",
        System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    private static readonly System.Text.RegularExpressions.Regex AffiliateBlocks = new(
        @"(?is)\b(?:Acestea sunt cele mai bune oferte de la partenerii noștri afiliați|These are the best offers from our affiliate partners|Dies sind die besten Angebote unserer Affiliate-Partner|Estas son las mejores ofertas de nuestros socios afiliados|Voici les meilleures offres de nos partenaires affiliés|Estas são as melhores ofertas dos nossos parceiros afiliados|Ezek a legjobb ajánlatok affiliate partnereinktől|Queste sono le migliori offerte dei nostri partner affiliati)\b.*$",
        System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    private static readonly System.Text.RegularExpressions.Regex PreferredSourceBlocks = new(
        @"(?is)\b(?:(?:Adaugă|Adăugați) ca sursă preferată pe Google|Add\s+as\s+(?:a\s+)?preferred\s+source\s+on\s+Google|Als bevorzugte Quelle bei Google hinzufügen|Añadir como fuente preferida en Google|Ajouter comme source préférée sur Google|Adicionar como fonte preferida no Google|Hozzáadás preferált forrásként a Google-ben|Aggiungi come fonte preferita su Google)\b.*$",
        System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    private static readonly System.Text.RegularExpressions.Regex RelatedArticleBlocks = new(
        @"(?is)\b(?:Articole înrudite|Related articles|Verwandte Artikel|Artículos relacionados|Articles connexes|Artigos relacionados|Kapcsolódó cikkek|Articoli correlati|Comentariile cititorilor|Reader comments|Leserkommentare|Comentarios de los lectores|Commentaires des lecteurs|Comentários dos leitores|Olvasói hozzászólások|Commenti dei lettori)\b.*$",
        System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    private static readonly System.Text.RegularExpressions.Regex HomeNavigationBlocks = new(
        @"(?is)\b(?:Anunțuri de acasă|Home\s+News\s+Reviews|Advertisements?\s+from\s+home|Werbung von der Startseite|Anuncios de inicio|Publicités de la page d’accueil|Anúncios da página inicial|Kezdőlap hirdetések|Pubblicità dalla home)\b.*$",
        System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

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
        using var xmlReader = System.Xml.XmlReader.Create(stream, new System.Xml.XmlReaderSettings
        {
            CheckCharacters = false,
            DtdProcessing = System.Xml.DtdProcessing.Ignore,
            Async = true
        });
        var xml = await XDocument.LoadAsync(xmlReader, LoadOptions.None, cancellationToken);
        var records = xml.Descendants().Where(element => element.Name.LocalName is "item" or "entry").ToList();
        if (records.Count == 0) throw new InvalidOperationException(UiText.Translate("Adresa nu conține un flux RSS sau Atom cu articole."));
        return records.Select(record =>
        {
            string Field(string localName) => record.Elements().FirstOrDefault(element => element.Name.LocalName == localName)?.Value ?? string.Empty;
            var html = Field("encoded");
            if (string.IsNullOrWhiteSpace(html)) html = Field("content");
            if (string.IsNullOrWhiteSpace(html)) html = Field("description");
            if (string.IsNullOrWhiteSpace(html)) html = Field("summary");

            var altLink = record.Elements()
                .FirstOrDefault(element => element.Name.LocalName == "link" && (element.Attribute("rel") == null || element.Attribute("rel")?.Value == "alternate"))
                ?.Attribute("href")?.Value;
            var link = !string.IsNullOrWhiteSpace(altLink)
                ? altLink
                : (record.Elements().FirstOrDefault(element => element.Name.LocalName == "link")?.Attribute("href")?.Value ?? Field("link"));

            var date = Field("pubDate");
            if (string.IsNullOrWhiteSpace(date)) date = Field("published");
            if (string.IsNullOrWhiteSpace(date)) date = Field("updated");
            var title = WebUtility.HtmlDecode(Field("title")).Trim();
            var id = Field("guid");
            if (string.IsNullOrWhiteSpace(id)) id = Field("id");
            if (string.IsNullOrWhiteSpace(id)) id = link;
            if (string.IsNullOrWhiteSpace(id)) id = $"{title}\n{date}";
            var text = ArticleTextNormalizer.ToReadableText(html);
            return new Article { Id = id, Title = title, Content = text, Link = link, Published = ParseRssDate(date) };
        }).OrderByDescending(item => item.Published).ToList();
    }

    public static DateTimeOffset ParseRssDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString)) return DateTimeOffset.Now;
        var trimmed = dateString.Trim();
        if (DateTimeOffset.TryParse(trimmed, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsed))
            return parsed;
        if (DateTimeOffset.TryParse(trimmed, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out parsed))
            return parsed;

        var normalized = System.Text.RegularExpressions.Regex.Replace(trimmed, @"(?i)\b(UT|GMT|UTC)\b", "+0000");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"(?i)\bEST\b", "-0500");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"(?i)\bEDT\b", "-0400");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"(?i)\bCST\b", "-0600");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"(?i)\bCDT\b", "-0500");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"(?i)\bMST\b", "-0700");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"(?i)\bMDT\b", "-0600");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"(?i)\bPST\b", "-0800");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"(?i)\bPDT\b", "-0700");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"(?i)\bCET\b", "+0100");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"(?i)\bCEST\b", "+0200");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"(?i)\bEET\b", "+0200");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"(?i)\bEEST\b", "+0300");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"(?i)\bBST\b", "+0100");

        if (DateTimeOffset.TryParse(normalized, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsed))
            return parsed;

        return DateTimeOffset.Now;
    }

    public async Task<string> LoadReadableContentAsync(string articleUrl)
    {
        using var response = await _client.GetAsync(articleUrl);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        return await Task.Run(() => CleanReadableContent(html));
    }

    /// <summary>Applies the same readable-content cleanup to cached HTML or text.</summary>
    public static string CleanReadableContent(string? content) =>
        string.IsNullOrWhiteSpace(content) ? string.Empty : ExtractReadableText(content);

    /// <summary>Also removes a title duplicated in the article body and standalone source metadata.</summary>
    public static string CleanReadableContent(string? content, string? articleTitle)
    {
        var text = CleanReadableContent(content);
        if (string.IsNullOrWhiteSpace(text)) return text;

        var title = ArticleTextNormalizer.ToReadableText(articleTitle);
        if (!string.IsNullOrWhiteSpace(title) && text.StartsWith(title, StringComparison.CurrentCultureIgnoreCase))
        {
            var remainder = text[title.Length..];
            if (remainder.Length == 0 || char.IsWhiteSpace(remainder[0]))
                text = remainder.TrimStart();
        }

        text = System.Text.RegularExpressions.Regex.Replace(text, @"(?im)^\s*(?:Source|Sursă)(?:\s*\([^\r\n)]*\))?\s*$", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, "(\\r?\\n\\s*){2,}", Environment.NewLine + Environment.NewLine);
        return text.Trim();
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
        article = System.Text.RegularExpressions.Regex.Replace(article, @"<(script|style|noscript|svg|nav|header|footer|aside)\b[^>]*>[\s\S]*?</\1\s*>", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        article = System.Text.RegularExpressions.Regex.Replace(article, @"<text\b[\s\S]*?</svg>", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        article = System.Text.RegularExpressions.Regex.Replace(article, "background-(?:position|repeat|image|size|color)\\s*:[^;]+;?", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        article = System.Text.RegularExpressions.Regex.Replace(article, @"\+(?=[<>])", string.Empty);
        article = System.Text.RegularExpressions.Regex.Replace(article, "<!--[\\s\\S]*?-->", " ");
        article = System.Text.RegularExpressions.Regex.Replace(article, "<(br|/p|/div|/li|/h[1-6])[^>]*>", Environment.NewLine, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var text = WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(article, "<[^>]+>", " "));
        text = WebUtility.HtmlDecode(text);
        // Reclamele livrate ca JavaScript escapate pot rămâne text după eliminarea tagurilor.
        text = System.Text.RegularExpressions.Regex.Replace(text, @"(?is)googletag\.cmd\.push\(function\(\).*?(?:\}\);|$)", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"(?is)<text\b.*?</svg>", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s*""\);\s*""\s*>\s*", " ");
        text = InlineAdLabels.Replace(text, " ");
        text = CommentLabels.Replace(text, " ");
        text = CommentCounts.Replace(text, " ");
        text = HomeNavigationBlocks.Replace(text, " ");
        text = AffiliateBlocks.Replace(text, " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"(?is)\b(?:Source\s*\([^)]*\)|Sursă\s*\([^)]*\))", " ");
        text = PreferredSourceBlocks.Replace(text, " ");
        text = RelatedArticleBlocks.Replace(text, " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"(?im)^\s*\d+\s*/\s*\d+\s*$", " ");
        // Subsolurile editoriale și meniurile de politică nu fac parte din articol.
        text = System.Text.RegularExpressions.Regex.Replace(text, @"(?is)\bO campanie editorială\b.*$", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, "[ \\t]+", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"[ \t]*\r?\n[ \t]*", Environment.NewLine);
        text = System.Text.RegularExpressions.Regex.Replace(text, "(\\r?\\n\\s*){2,}", Environment.NewLine + Environment.NewLine);
        return text.Trim();
    }
}
