using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

/// <summary>Localizes only the built-in AI instruction while preserving user-customized text.</summary>
public static class AiInstructionsLocalization
{
    public static string ForDisplay(string? savedInstructions)
    {
        if (string.Equals(savedInstructions?.Trim(), AppSettings.DefaultAiInstructions, StringComparison.Ordinal))
            return UiText.Translate(AppSettings.DefaultAiInstructions);

        return savedInstructions ?? string.Empty;
    }

    public static string ForStorage(string? displayedInstructions)
    {
        var value = displayedInstructions?.Trim() ?? string.Empty;
        return string.Equals(value, UiText.Translate(AppSettings.DefaultAiInstructions), StringComparison.Ordinal)
            ? AppSettings.DefaultAiInstructions
            : value;
    }
}
