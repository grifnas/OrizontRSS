using System.Text.RegularExpressions;

namespace CititorRSS.Jaws.Services;

/// <summary>
/// Înlocuiește valorile secrete care pot apărea în textul unei excepții: parametri de adresă (key, token, session),
/// antete de autentificare și cookie-uri, precum și chei în formatul furnizorilor folosiți de aplicație.
/// Este o plasă de siguranță euristică, nu o garanție; textul obișnuit rămâne neschimbat.
/// </summary>
internal static class SensitiveText
{
    private const string Replacement = "[ascuns]";

    private static readonly Regex[] SecretValuePatterns =
    [
        // Parametrii de adresă cu nume sugestiv: ?key=…, &access_token=…
        new(@"(?<name>[?&](?:api[_-]?key|apikey|key|token|access[_-]?token|auth|session(?:id)?|password|secret)=)[^&\s""'<>]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        // Antetele Authorization: Bearer …, Basic … (cu majuscule, cum apar în antete; fără IgnoreCase, ca „basic settings” să rămână)
        new(@"(?<name>\b(?:Bearer|Basic)\s+)[A-Za-z0-9._~+/=-]+", RegexOptions.CultureInvariant),
        // Cookie și Set-Cookie: totul până la sfârșitul rândului
        new(@"(?<name>\b(?:Set-)?Cookie\s*[:=]\s*)[^\r\n]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        // Chei Google (AIza…) și OpenAI (sk-…), oricând apar în text
        new(@"\bAIza[0-9A-Za-z_-]{30,}", RegexOptions.CultureInvariant),
        new(@"\bsk-[A-Za-z0-9_-]{16,}", RegexOptions.CultureInvariant),
    ];

    internal static string Redact(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var result = text;
        foreach (var pattern in SecretValuePatterns)
        {
            result = pattern.Replace(result, match =>
                match.Groups["name"].Success ? match.Groups["name"].Value + Replacement : Replacement);
        }
        return result;
    }
}
