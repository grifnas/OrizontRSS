using System.Globalization;
using System.IO;
using System.Linq;

namespace CititorRSS.Jaws.Localization;

public static class UserGuideLocator
{
    private const string BaseName = "Ghid-utilizator-Orizont-RSS";

    public static string FileNameFor(CultureInfo culture)
    {
        var language = culture.TwoLetterISOLanguageName.ToLowerInvariant();
        return language is "en" or "es" or "fr" or "de" or "pt" or "hu" or "it" ? $"{BaseName}.{language}.html" : $"{BaseName}.html";
    }

    public static string? Find()
    {
        var fileName = FileNameFor(CultureInfo.CurrentUICulture);
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "docs", "user-guides", fileName),
            Path.Combine(AppContext.BaseDirectory, fileName),
            Path.Combine(AppContext.BaseDirectory, "docs", "user-guides", $"{BaseName}.html"),
            Path.Combine(AppContext.BaseDirectory, $"{BaseName}.html")
        };
        return candidates.FirstOrDefault(File.Exists);
    }
}
