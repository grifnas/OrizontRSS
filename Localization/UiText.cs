using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;

namespace CititorRSS.Jaws.Localization;

public static class UiText
{
    private static readonly ResourceManager Resources = new("CititorRSS.Jaws.Resources.UiStrings", typeof(UiText).Assembly);

    public static string Translate(string? source)
    {
        if (string.IsNullOrEmpty(source)) return source ?? string.Empty;
        if (string.Equals(source, "toate folderele", StringComparison.Ordinal))
        {
            var folderLabel = Resources.GetString("Toate folderele", CultureInfo.CurrentUICulture);
            if (folderLabel is not null) return folderLabel.ToLower(CultureInfo.CurrentUICulture);
        }
        var translated = Resources.GetString(source, CultureInfo.CurrentUICulture);
        if (translated is null)
        {
            var normalizedKey = Regex.Replace(source, @"\s+", " ");
            if (!string.Equals(normalizedKey, source, StringComparison.Ordinal))
                translated = Resources.GetString(normalizedKey, CultureInfo.CurrentUICulture);
        }
        return translated ?? source;
    }

    public static string Format(string source, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Translate(source), arguments);
}
