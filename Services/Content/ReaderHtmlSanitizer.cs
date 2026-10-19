using System.Net;
using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace CititorRSS.Jaws;

/// <summary>
/// Păstrează din articolul ales de SmartReader numai structura utilă la citire (subtitluri, paragrafe,
/// liste, citate, tabele și linkuri web), pentru WebReader. Orice altceva (scripturi, stiluri, formulare,
/// imagini, atribute) este eliminat, iar elementele de grupare sunt înlocuite cu conținutul lor.
/// </summary>
public static class ReaderHtmlSanitizer
{
    private static readonly HashSet<string> KeptElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "ul", "ol", "li", "blockquote", "strong", "em", "b", "i", "br",
        "table", "caption", "thead", "tbody", "tfoot", "tr", "th", "td",
        "figure", "figcaption", "pre", "code", "dl", "dt", "dd", "sub", "sup"
    };

    // Conținutul acestor elemente nu este text de citit sau poate rula cod; se elimină cu totul.
    private static readonly HashSet<string> DroppedElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "noscript", "template", "iframe", "frame", "object", "embed", "applet",
        "form", "input", "button", "select", "textarea", "option", "label",
        "svg", "math", "canvas", "video", "audio", "source", "track", "picture", "img", "map",
        "nav", "aside", "footer", "head", "link", "meta", "base"
    };

    public static string Sanitize(string html, string pageUrl)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var document = new HtmlParser().ParseDocument($"<html><body>{html}</body></html>");
        Uri.TryCreate(pageUrl, UriKind.Absolute, out var baseUri);
        var output = new StringBuilder(html.Length);
        if (document.Body is not null)
            foreach (var child in document.Body.ChildNodes) Append(child, baseUri, output);
        return output.ToString().Trim();
    }

    private static void Append(INode node, Uri? baseUri, StringBuilder output)
    {
        if (node.NodeType == NodeType.Text)
        {
            output.Append(WebUtility.HtmlEncode(node.TextContent));
            return;
        }
        if (node is not IElement element) return;

        var name = element.LocalName.ToLowerInvariant();
        if (DroppedElements.Contains(name)) return;

        // Pagina are deja titlul articolului ca h1; subtitlurile încep de la h2.
        if (name.Length == 2 && name[0] == 'h' && name[1] is >= '1' and <= '6')
        {
            var level = Math.Max(2, name[1] - '0');
            output.Append("<h").Append(level).Append('>');
            AppendChildren(element, baseUri, output);
            output.Append("</h").Append(level).Append(">\n");
            return;
        }

        if (name == "a")
        {
            var href = SafeLink(element.GetAttribute("href"), baseUri);
            if (href is null) { AppendChildren(element, baseUri, output); return; }
            output.Append("<a href=\"").Append(WebUtility.HtmlEncode(href)).Append("\">");
            AppendChildren(element, baseUri, output);
            output.Append("</a>");
            return;
        }

        if (name == "br") { output.Append("<br>"); return; }

        if (KeptElements.Contains(name))
        {
            output.Append('<').Append(name).Append('>');
            AppendChildren(element, baseUri, output);
            output.Append("</").Append(name).Append('>');
            if (name is "p" or "ul" or "ol" or "li" or "blockquote" or "table" or "tr" or "figure" or "pre" or "dl") output.Append('\n');
            return;
        }

        // div, span, section, article și celelalte: rămâne doar conținutul.
        AppendChildren(element, baseUri, output);
    }

    private static void AppendChildren(IElement element, Uri? baseUri, StringBuilder output)
    {
        foreach (var child in element.ChildNodes) Append(child, baseUri, output);
    }

    private static string? SafeLink(string? href, Uri? baseUri)
    {
        if (string.IsNullOrWhiteSpace(href)) return null;
        Uri? target = null;
        if (!Uri.TryCreate(href.Trim(), UriKind.Absolute, out target) && baseUri is not null)
            Uri.TryCreate(baseUri, href.Trim(), out target);
        return target is not null && (target.Scheme == Uri.UriSchemeHttp || target.Scheme == Uri.UriSchemeHttps)
            ? target.AbsoluteUri
            : null;
    }
}
