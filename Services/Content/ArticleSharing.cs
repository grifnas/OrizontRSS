namespace CititorRSS.Jaws;

public static class ArticleSharing
{
    public const string PresentationUrl = "https://grifnas.github.io/OrizontRSS/";
    public const int EmailBodyLimit = 1800;
    public const int WhatsAppBodyLimit = 3500;

    public static string BuildShareText(string title, string? content, string? link)
        => BuildShareText(title, content, link, null, null, null);

    public static string BuildShareText(string title, string? content, string? link, string? translationNote, string? appAttribution, string? sourceLabel)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(title)) parts.Add(title.Trim());
        if (!string.IsNullOrWhiteSpace(content)) parts.Add(content.Trim());
        var footer = BuildFooter(appAttribution, sourceLabel, link, translationNote);
        if (!string.IsNullOrWhiteSpace(footer)) parts.Add(footer);
        return string.Join(Environment.NewLine + Environment.NewLine, parts);
    }

    public static string BuildFooter(string? appAttribution, string? sourceLabel, string? link, string? translationNote = null)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(appAttribution))
            parts.Add($"{appAttribution.Trim()}{Environment.NewLine}{PresentationUrl}");
        if (!string.IsNullOrWhiteSpace(translationNote)) parts.Add(translationNote.Trim());
        if (!string.IsNullOrWhiteSpace(link) && !string.IsNullOrWhiteSpace(sourceLabel)) parts.Add($"{sourceLabel.Trim()}{Environment.NewLine}{link.Trim()}");
        return string.Join(Environment.NewLine + Environment.NewLine, parts);
    }

    public static string LimitForUri(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;
        const string marker = "\n\n[...]";
        var contentLength = Math.Max(0, maxLength - marker.Length);
        return text[..Math.Min(contentLength, text.Length)] + marker;
    }

    public static string LimitForUri(string text, int maxLength, string? preservedSuffix)
    {
        if (string.IsNullOrWhiteSpace(preservedSuffix) || text.Length <= maxLength) return LimitForUri(text, maxLength);
        const string marker = "\n\n[...]";
        var separator = Environment.NewLine + Environment.NewLine;
        var suffixStart = text.LastIndexOf(preservedSuffix, StringComparison.Ordinal);
        if (suffixStart < 0) return LimitForUri(text, maxLength);
        var prefix = text[..Math.Max(0, suffixStart - separator.Length)].TrimEnd();
        var requiredLength = marker.Length + separator.Length + preservedSuffix.Length;
        var prefixLength = Math.Max(0, maxLength - requiredLength);
        return prefix[..Math.Min(prefixLength, prefix.Length)] + marker + separator + preservedSuffix;
    }

    public static string CreateMailto(string subject, string body) =>
        $"mailto:?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";

    public static string CreateWhatsApp(string text) =>
        $"https://wa.me/?text={Uri.EscapeDataString(text)}";
}
