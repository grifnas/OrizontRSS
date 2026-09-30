namespace CititorRSS.Jaws;

public partial class MainWindow
{
    // Callbacks keep the startup orchestration testable without touching a profile
    // or a server. The refresh callback obtains the feed list after mirroring.
    internal async Task RunStartupUpdatesAsync(Func<Task> mirrorFeeds, Func<bool, Task> refreshArticles)
    {
        if (_closeInProgress || _isClosingAfterSave) return;
        // Periodic synchronization must not override the independent startup switches.
        var connected = _settings.NewsBlurConnected && !string.IsNullOrWhiteSpace(_settings.EncryptedNewsBlurSession);
        var mirror = connected && _settings.NewsBlurSyncFeedsAndFoldersAtStartup;
        var newsBlurArticles = connected && _settings.NewsBlurSyncArticlesAndStatesAtStartup;
        var refresh = newsBlurArticles || _settings.UpdateAtStartup;
        if (mirror)
            await mirrorFeeds();
        if (_closeInProgress || _isClosingAfterSave) return;
        if (refresh)
            await refreshArticles(newsBlurArticles);
    }
}
