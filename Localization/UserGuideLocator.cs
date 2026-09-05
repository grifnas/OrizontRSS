using System.Globalization;
using System.IO;

namespace CititorRSS.Jaws.Localization;

public static class UserGuideLocator
{
    private const string BaseName = "Ghid-utilizator-Orizont-RSS";

    public static string FileNameFor(CultureInfo culture)
    {
        var language = culture.TwoLetterISOLanguageName.ToLowerInvariant();
        return language is "en" or "es" or "fr" or "de" or "pt" ? $"{BaseName}.{language}.html" : $"{BaseName}.html";
    }

    public static string? Find()
    {
        var preferred = Path.Combine(AppContext.BaseDirectory, FileNameFor(CultureInfo.CurrentUICulture));
        if (File.Exists(preferred)) return preferred;
        var fallback = Path.Combine(AppContext.BaseDirectory, $"{BaseName}.html");
        return File.Exists(fallback) ? fallback : null;
    }
}
