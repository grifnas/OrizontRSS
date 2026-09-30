using System.IO;
using System.Text;
using System.Globalization;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws.Services.Content;

public static class ArticleExportService
{
    private const string DefaultAppAttribution = "Conținut preluat prin Orizont RSS:";
    private const string DefaultSourceLabel = "Sursa articolului:";

    public static string NormalizeFormat(string? format) =>
        string.Equals(format, "rtf", StringComparison.OrdinalIgnoreCase) ? "rtf" : "txt";

    public static string Save(Article article, string content, string folder, string? sourceName, string format)
    {
        ArgumentNullException.ThrowIfNull(article);
        return Save(article, new ArticleDistributionContext(article.Title, content, content, article.Link), folder, sourceName, format,
            T(DefaultAppAttribution), T(DefaultSourceLabel), null);
    }

    public static string Save(
        Article article,
        ArticleDistributionContext context,
        string folder,
        string? sourceName,
        string format,
        string appAttribution,
        string sourceLabel,
        string? translationNote)
    {
        if (article is null) throw new ArgumentNullException(nameof(article));
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException(T("Folderul de export lipsește."));

        Directory.CreateDirectory(folder);
        var extension = NormalizeFormat(format);
        var filePath = GetUniquePath(folder, article.Title, extension);
        var text = BuildText(article, context, sourceName, appAttribution, sourceLabel, translationNote);
        if (extension == "rtf")
            File.WriteAllText(filePath, ToRtf(text, article.Title), new UTF8Encoding(false));
        else
            File.WriteAllText(filePath, text, new UTF8Encoding(false));
        return filePath;
    }

    public static string BuildText(Article article, string? content, string? sourceName)
    {
        ArgumentNullException.ThrowIfNull(article);
        var text = content ?? article.FullContent ?? article.Content;
        return BuildText(article,
            new ArticleDistributionContext(article.Title, text, text, article.Link),
            sourceName, T(DefaultAppAttribution), T(DefaultSourceLabel), null);
    }

    public static string BuildText(
        Article article,
        ArticleDistributionContext context,
        string? sourceName,
        string appAttribution,
        string sourceLabel,
        string? translationNote)
    {
        ArgumentNullException.ThrowIfNull(article);
        ArgumentNullException.ThrowIfNull(context);
        var lines = new List<string>
        {
            article.Title.Trim(),
            string.Empty,
            F("Data publicării: {0}", article.Published.ToString("dd MMMM yyyy, HH:mm", CultureInfo.CurrentUICulture))
        };
        if (!string.IsNullOrWhiteSpace(sourceName)) lines.Add(F("Feed: {0}", sourceName));
        lines.Add(string.Empty);
        lines.Add(string.IsNullOrWhiteSpace(context.DisplayText) ? article.FullContent ?? article.Content : context.DisplayText.Trim());
        var document = string.Join(Environment.NewLine, lines);
        var footer = ArticleSharing.BuildFooter(context, translationNote, appAttribution, sourceLabel);
        return ArticleSharing.AppendFooter(document, footer);
    }

    public static string GetUniquePath(string folder, string? title, string extension)
    {
        var baseName = SanitizeFileName(title);
        var path = Path.Combine(folder, $"{baseName}.{extension}");
        var number = 2;
        while (File.Exists(path))
        {
            path = Path.Combine(folder, $"{baseName} ({number}).{extension}");
            number++;
        }
        return path;
    }

    public static string SanitizeFileName(string? title)
    {
        var fallbackTitle = T("Articol");
        var value = string.IsNullOrWhiteSpace(title) ? fallbackTitle : title.Trim();
        var invalid = Path.GetInvalidFileNameChars();
        value = new string(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray()).TrimEnd('.', ' ');
        if (string.IsNullOrWhiteSpace(value)) value = fallbackTitle;
        return value.Length > 140 ? value[..140].TrimEnd('.', ' ') : value;
    }

    private static string T(string source) => UiText.Translate(source);
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);

    private static string ToRtf(string text, string title)
    {
        var builder = new StringBuilder("{\\rtf1\\ansi\\deff0{\\fonttbl{\\f0 Calibri;}}\\viewkind4\\uc1\\fs24 ");
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            if (index == 0) builder.Append("\\b ");
            builder.Append(EscapeRtf(lines[index]));
            if (index == 0) builder.Append("\\b0");
            if (index < lines.Length - 1) builder.Append("\\par ");
        }
        builder.Append('}');
        return builder.ToString();
    }

    private static string EscapeRtf(string value)
    {
        var builder = new StringBuilder();
        foreach (var character in value)
        {
            switch (character)
            {
                case '\\': builder.Append(@"\\"); break;
                case '{': builder.Append(@"\{"); break;
                case '}': builder.Append(@"\}"); break;
                case '\t': builder.Append(@"\tab "); break;
                default:
                    if (character >= 32 && character <= 126) builder.Append(character);
                    else builder.Append($"\\u{(short)character}?");
                    break;
            }
        }
        return builder.ToString();
    }
}
