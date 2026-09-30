using System.Globalization;

namespace CititorRSS.Jaws.Services.Content;

/// <summary>
/// Immutable provenance snapshot for the text currently being displayed or shared.
/// OriginalText remains available even when DisplayText is a translation.
/// </summary>
public sealed record ArticleDistributionContext(
    string Title,
    string OriginalText,
    string DisplayText,
    string? SourceUrl,
    string? TranslationProvider = null,
    string? TranslationLanguage = null)
{
    public bool IsTranslated => !string.IsNullOrWhiteSpace(TranslationProvider);

    public string? BuildTranslationNote(string format)
    {
        if (!IsTranslated || string.IsNullOrWhiteSpace(format)) return null;
        var language = string.IsNullOrWhiteSpace(TranslationLanguage)
            ? CultureInfo.CurrentUICulture.NativeName
            : TranslationLanguage;
        return string.Format(CultureInfo.CurrentUICulture, format, TranslationProvider, language);
    }

    public static string LanguageNameForCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode)) return CultureInfo.CurrentUICulture.NativeName;
        var normalized = languageCode.Trim().Replace('_', '-');
        try { return CultureInfo.GetCultureInfo(normalized).DisplayName; }
        catch (CultureNotFoundException) { return normalized; }
    }
}
