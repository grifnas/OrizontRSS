using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace CititorRSS.Jaws;

/// <summary>
/// Bara de stare a unei ferestre secundare. Fiecare fereastră are o <c>StatusBar</c> numită
/// <c>WindowStatusBar</c> cu un <c>TextBlock</c> numit <c>WindowStatus</c>; cititorul de ecran o citește
/// la deschidere și la fiecare mesaj nou.
/// </summary>
internal static class WindowStatusAnnouncer
{
    /// <summary>Anunță mesajul la încărcarea ferestrei și îl afișează în bara de stare.</summary>
    public static void Attach(Window window, Func<string> message) =>
        window.Loaded += (_, _) => Set(window, message());

    public static void Set(Window window, string message)
    {
        if (window.FindName("WindowStatus") is not TextBlock text) return;
        if (window.FindName("WindowStatusBar") is not StatusBar bar) return;
        StatusAnnouncer.Set(text, message, bar);
    }
}
