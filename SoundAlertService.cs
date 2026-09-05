using System.Media;

namespace CititorRSS.Jaws;

internal static class SoundAlertService
{
    private static DateTimeOffset? _lastQuietSuccessAlert;

    public static void RefreshFinished(bool enabled, bool alertOnSuccess, bool alertOnNewArticles, bool alertOnErrors, bool hasErrors, int newArticleCount)
    {
        if (!enabled) return;
        if (hasErrors)
        {
            _lastQuietSuccessAlert = null;
            if (alertOnErrors) SystemSounds.Exclamation.Play();
            return;
        }

        if (newArticleCount > 0)
        {
            _lastQuietSuccessAlert = null;
            if (alertOnNewArticles || alertOnSuccess) SystemSounds.Asterisk.Play();
            return;
        }

        // A refresh without new articles is intentionally throttled, so repeated
        // manual or startup refreshes do not produce an annoying sound loop.
        if (alertOnSuccess && (!_lastQuietSuccessAlert.HasValue || DateTimeOffset.UtcNow - _lastQuietSuccessAlert.Value >= TimeSpan.FromMinutes(5)))
        {
            SystemSounds.Asterisk.Play();
            _lastQuietSuccessAlert = DateTimeOffset.UtcNow;
        }
    }

    public static void Test()
    {
        SystemSounds.Asterisk.Play();
    }
}
