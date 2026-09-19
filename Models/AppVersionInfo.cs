using System.Reflection;
using System.Text.RegularExpressions;

namespace CititorRSS.Jaws;

/// <summary>O singură sursă pentru versiunea afișată în ferestre, mesaje și diagnostice.</summary>
public static class AppVersionInfo
{
    public static string InformationalVersion { get; } =
        typeof(AppVersionInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(AppVersionInfo).Assembly.GetName().Version?.ToString(3)
        ?? "necunoscută";

    public static string DisplayVersion { get; } = ToDisplayVersion(InformationalVersion);
    public static string ProductTitle => $"Orizont RSS {DisplayVersion}";

    private static string ToDisplayVersion(string value)
    {
        var clean = value.Split('+', 2)[0];
        var preview = Regex.Match(clean, @"^(?<version>\d+(?:\.\d+)+)-preview\.(?<number>\d+)$", RegexOptions.IgnoreCase);
        return preview.Success
            ? $"{preview.Groups["version"].Value} — Preview {preview.Groups["number"].Value}"
            : clean;
    }
}
