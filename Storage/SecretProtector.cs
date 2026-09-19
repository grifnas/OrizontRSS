using System.Security.Cryptography;
using System.Text;

namespace CititorRSS.Jaws;

public static class SecretProtector
{
    // This value is intentionally retained so Gemini keys saved by earlier versions remain readable.
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("CititorRSS-JAWS-OpenAI-Key-v1");
    public static string Protect(string value) => Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(value), Entropy, DataProtectionScope.CurrentUser));
    public static string Unprotect(string? protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue)) return string.Empty;
        return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(protectedValue), Entropy, DataProtectionScope.CurrentUser));
    }
}
