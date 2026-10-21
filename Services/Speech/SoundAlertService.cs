using System.IO;
using System.Media;
namespace CititorRSS.Jaws;

/// <summary>Evenimentele care pot avea sunet. Fișierul fiecărui eveniment poartă numele din <see cref="SoundAlertService.FileName"/>.</summary>
public enum SoundEvent
{
    NewArticles,
    RefreshDone,
    Error,
    FolderChanged,
    ListEdge,
    FavoriteAdded,
    FavoriteRemoved,
    ArticleDeleted,
    AiReady,
    SyncDone
}

public static class SoundAlertService
{
    private static DateTimeOffset? _lastQuietSuccessAlert;
    private static readonly Dictionary<string, SoundPlayer> Players = new(StringComparer.OrdinalIgnoreCase);
    private static bool _enabled = true;
    private static string _theme = SoundThemeIds.Windows;

    /// <summary>Comutatorul general și tema aleasă în Setări; se aplică tuturor sunetelor aplicației.</summary>
    public static void Configure(bool enabled, string? theme)
    {
        _enabled = enabled;
        _theme = SoundThemeIds.Normalize(theme);
    }

    public static string FileName(SoundEvent soundEvent) => soundEvent switch
    {
        SoundEvent.NewArticles => "new-articles",
        SoundEvent.RefreshDone => "refresh-done",
        SoundEvent.Error => "error",
        SoundEvent.FolderChanged => "folder-changed",
        SoundEvent.ListEdge => "list-edge",
        SoundEvent.FavoriteAdded => "favorite-added",
        SoundEvent.FavoriteRemoved => "favorite-removed",
        SoundEvent.ArticleDeleted => "article-deleted",
        SoundEvent.AiReady => "ai-ready",
        _ => "sync-done"
    };

    /// <summary>Calea fișierului pentru o temă proprie; tema Windows nu are fișiere.</summary>
    public static string? SoundPath(string theme, SoundEvent soundEvent)
    {
        var normalized = SoundThemeIds.Normalize(theme);
        if (normalized == SoundThemeIds.Windows) return null;
        return Path.Combine(AppContext.BaseDirectory, "Sounds", normalized.ToLowerInvariant(), FileName(soundEvent) + ".wav");
    }

    public static void Play(SoundEvent soundEvent)
    {
        if (_enabled) PlayTheme(_theme, soundEvent);
    }

    /// <summary>Redă sunetul unei teme fără să țină cont de comutator, pentru butonul de test din Setări.</summary>
    public static void Preview(string? theme, SoundEvent soundEvent = SoundEvent.NewArticles) =>
        PlayTheme(SoundThemeIds.Normalize(theme), soundEvent);

    private static void PlayTheme(string theme, SoundEvent soundEvent)
    {
        if (theme == SoundThemeIds.Windows)
        {
            // Tema Windows păstrează sunetele de sistem de până acum; acțiunile noi rămân fără sunet.
            var systemSound = soundEvent switch
            {
                SoundEvent.NewArticles or SoundEvent.RefreshDone => SystemSounds.Asterisk,
                SoundEvent.Error => SystemSounds.Exclamation,
                SoundEvent.FolderChanged => SystemSounds.Question,
                SoundEvent.ListEdge => SystemSounds.Beep,
                _ => null
            };
            systemSound?.Play();
            return;
        }
        var path = SoundPath(theme, soundEvent);
        if (path is null || !File.Exists(path)) return;
        try
        {
            if (!Players.TryGetValue(path, out var player))
            {
                player = new SoundPlayer(path);
                player.Load();
                Players[path] = player;
            }
            player.Play();
        }
        catch
        {
            // Un fișier de sunet lipsă sau deteriorat nu trebuie să întrerupă lucrul în aplicație.
        }
    }

    public static void RefreshFinished(bool enabled, bool alertOnSuccess, bool alertOnNewArticles, bool alertOnErrors, bool hasErrors, int newArticleCount)
    {
        if (!enabled) return;
        if (hasErrors) { _lastQuietSuccessAlert = null; if (alertOnErrors) PlayTheme(_theme, SoundEvent.Error); return; }
        if (newArticleCount > 0) { _lastQuietSuccessAlert = null; if (alertOnNewArticles || alertOnSuccess) PlayTheme(_theme, SoundEvent.NewArticles); return; }
        if (alertOnSuccess && (!_lastQuietSuccessAlert.HasValue || System.DateTimeOffset.UtcNow - _lastQuietSuccessAlert.Value >= System.TimeSpan.FromMinutes(5)))
        { PlayTheme(_theme, SoundEvent.RefreshDone); _lastQuietSuccessAlert = System.DateTimeOffset.UtcNow; }
    }
    public static void Test() => Preview(_theme);
    public static void PlayFolderChanged() => Play(SoundEvent.FolderChanged);
    public static void PlayListEnd() => Play(SoundEvent.ListEdge);
}
