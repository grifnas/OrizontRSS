namespace CititorRSS.Jaws;

internal static class ArticleSharing
{
    public const int EmailBodyLimit = 1800;
    public const int WhatsAppBodyLimit = 3500;

    public static string BuildShareText(string title, string? content, string? link)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(title)) parts.Add(title.Trim());
        if (!string.IsNullOrWhiteSpace(content)) parts.Add(content.Trim());
        if (!string.IsNullOrWhiteSpace(link)) parts.Add($"Sursa: {link.Trim()}");
        return string.Join(Environment.NewLine + Environment.NewLine, parts);
    }

    public static string LimitForUri(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;
        const string marker = "\n\n[...]";
        var contentLength = Math.Max(0, maxLength - marker.Length);
        return text[..Math.Min(contentLength, text.Length)] + marker;
    }

    public static string CreateMailto(string subject, string body) =>
        $"mailto:?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";

    public static string CreateWhatsApp(string text) =>
        $"https://wa.me/?text={Uri.EscapeDataString(text)}";
}
