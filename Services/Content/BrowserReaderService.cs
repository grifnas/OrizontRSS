using System.Diagnostics;
using System.IO;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

internal enum BrowserOpenMode
{
    ReadingMode,
    DefaultBrowser
}

/// <summary>Deschide articolele în modul Citire Microsoft Edge, cu revenire sigură la browserul implicit.</summary>
internal static class BrowserReaderService
{
    public static BrowserOpenMode OpenReadingMode(string address)
    {
        var uri = ValidateWebAddress(address);
        var edgePath = FindMicrosoftEdge();
        if (edgePath is null)
        {
            OpenDefault(uri);
            return BrowserOpenMode.DefaultBrowser;
        }

        var startInfo = new ProcessStartInfo(edgePath) { UseShellExecute = true };
        // Microsoft documentează prefixul read: pentru deschiderea Immersive Reader.
        startInfo.ArgumentList.Add($"read:{uri.AbsoluteUri}");
        Process.Start(startInfo);
        return BrowserOpenMode.ReadingMode;
    }

    public static void OpenOriginal(string address) => OpenDefault(ValidateWebAddress(address));

    private static Uri ValidateWebAddress(string address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException(UiText.Translate("Adresa articolului nu este o adresă web HTTP sau HTTPS validă."));
        return uri;
    }

    private static void OpenDefault(Uri uri) =>
        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });

    private static string? FindMicrosoftEdge()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "Application", "msedge.exe")
        };
        return candidates.FirstOrDefault(File.Exists);
    }
}
