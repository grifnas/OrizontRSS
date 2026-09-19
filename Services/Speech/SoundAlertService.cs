using System.IO;
using System.Media;
namespace CititorRSS.Jaws;
internal static class SoundAlertService
{
    private static DateTimeOffset? _lastQuietSuccessAlert;
    public static void RefreshFinished(bool enabled, bool alertOnSuccess, bool alertOnNewArticles, bool alertOnErrors, bool hasErrors, int newArticleCount)
    {
        if (!enabled) return;
        if (hasErrors) { _lastQuietSuccessAlert = null; if (alertOnErrors) SystemSounds.Exclamation.Play(); return; }
        if (newArticleCount > 0) { _lastQuietSuccessAlert = null; if (alertOnNewArticles || alertOnSuccess) SystemSounds.Asterisk.Play(); return; }
        if (alertOnSuccess && (!_lastQuietSuccessAlert.HasValue || System.DateTimeOffset.UtcNow - _lastQuietSuccessAlert.Value >= System.TimeSpan.FromMinutes(5)))
        { SystemSounds.Asterisk.Play(); _lastQuietSuccessAlert = System.DateTimeOffset.UtcNow; }
    }
    public static void Test() { SystemSounds.Asterisk.Play(); }
    public static void PlayFolderChanged() { SystemSounds.Question.Play(); }
    public static void PlayListEnd() { SystemSounds.Beep.Play(); }
}
