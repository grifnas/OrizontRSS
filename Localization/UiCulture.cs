using System.Globalization;

namespace CititorRSS.Jaws.Localization;

public static class UiCulture
{
    public const string Automatic = "auto";

    public static IReadOnlyList<UiLanguage> SupportedLanguages { get; } =
    [
        new(Automatic, "Automat, după limba Windows"),
        new("ro-RO", "Română"),
        new("en-US", "English"),
        new("es-ES", "Español"),
        new("fr-FR", "Français"),
        new("de-DE", "Deutsch"),
        new("pt-BR", "Português (Brasil)")
    ];

    public static string NormalizeSelection(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return Automatic;
        return SupportedLanguages.Any(language => string.Equals(language.Code, code, StringComparison.OrdinalIgnoreCase))
            ? SupportedLanguages.First(language => string.Equals(language.Code, code, StringComparison.OrdinalIgnoreCase)).Code
            : Automatic;
    }

    public static string Resolve(string? selection)
    {
        var normalized = NormalizeSelection(selection);
        if (!string.Equals(normalized, Automatic, StringComparison.OrdinalIgnoreCase)) return normalized;

        var windows = CultureInfo.InstalledUICulture;
        return windows.TwoLetterISOLanguageName.ToLowerInvariant() switch
        {
            "ro" => "ro-RO",
            "en" => "en-US",
            "es" => "es-ES",
            "fr" => "fr-FR",
            "de" => "de-DE",
            "pt" => "pt-BR",
            _ => "en-US"
        };
    }

    public static void Apply(string? selection)
    {
        var culture = CultureInfo.GetCultureInfo(Resolve(selection));
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}

public sealed record UiLanguage(string Code, string NativeName)
{
    public override string ToString() => NativeName;
}
