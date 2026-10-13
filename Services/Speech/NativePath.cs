using System.Runtime.InteropServices;
using System.Text;

namespace CititorRSS.Jaws;

/// <summary>
/// eSpeak NG primește calea datelor ca șir de 8 biți: orice literă non-ASCII (ș, ț, ă, î) o strică,
/// iar unele produc chiar oprirea procesului. Căile ASCII trec neschimbate; altfel se folosește
/// numele scurt Windows (8.3), iar dacă nici acesta nu este ASCII, motorul nu este apelat.
/// </summary>
public static class NativePath
{
    public static bool IsAscii(string path) => path.All(character => character < 128);

    public static string? ForAnsiNativeApi(string path)
    {
        if (IsAscii(path)) return path;
        var buffer = new StringBuilder(1024);
        var length = GetShortPathNameW(path, buffer, (uint)buffer.Capacity);
        if (length == 0 || length >= buffer.Capacity) return null;
        var shortPath = buffer.ToString();
        return IsAscii(shortPath) ? shortPath : null;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetShortPathNameW(string longPath, StringBuilder shortPath, uint bufferLength);
}
