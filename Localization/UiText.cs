using System.Globalization;
using System.Resources;

namespace CititorRSS.Jaws.Localization;

public static class UiText
{
    private static readonly ResourceManager Resources = new("CititorRSS.Jaws.Resources.UiStrings", typeof(UiText).Assembly);

    public static string Translate(string? source)
    {
        if (string.IsNullOrEmpty(source)) return source ?? string.Empty;
        return Resources.GetString(source, CultureInfo.CurrentUICulture) ?? source;
    }

    public static string Format(string source, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Translate(source), arguments);
}
