namespace CititorRSS.Jaws;

/// <summary>Construiește setările sigure pentru un backup, fără cheile API.</summary>
public static class BackupPolicy
{
    public static AppSettings SanitizeSettings(AppSettings settings) => new()
    {
        UiLanguage = settings.UiLanguage,
        AutoCleanupEnabled = settings.AutoCleanupEnabled,
        RetentionDays = settings.RetentionDays,
        UpdateAtStartup = settings.UpdateAtStartup,
        AiInstructions = settings.AiInstructions,
        GeminiEnabled = false,
        EncryptedGeminiKey = null,
        DeepLEnabled = false,
        EncryptedDeepLKey = null,
        NewsBlurConnected = false,
        NewsBlurUsername = null,
        EncryptedNewsBlurSession = null,
        NewsBlurBootstrapCompletedUsername = null,
        NewsBlurSavedStoryMode = settings.NewsBlurSavedStoryMode,
        NewsBlurAutoSyncEnabled = settings.NewsBlurAutoSyncEnabled,
        NewsBlurAutoSyncMinutes = settings.NewsBlurAutoSyncMinutes,
        NewsBlurPendingFeedDeletions = (settings.NewsBlurPendingFeedDeletions ?? []).Where(item => item is not null).Select(item => new NewsBlurPendingFeedDeletion
        {
            FeedId = item.FeedId,
            Url = item.Url,
            Name = item.Name,
            Folder = item.Folder,
            QueuedAt = item.QueuedAt
        }).ToList(),
        LastFolder = settings.LastFolder,
        LastFeedId = settings.LastFeedId,
        LastArticleId = settings.LastArticleId,
        LastArticleLink = settings.LastArticleLink,
        LastArticleFilter = settings.LastArticleFilter,
        LastTimeFilter = settings.LastTimeFilter,
        LastTagFilter = settings.LastTagFilter,
        LastView = settings.LastView,
        LastPanel = settings.LastPanel,
        LastAttentionFilter = settings.LastAttentionFilter,
        HideRepeatedArticlesInGlobalViews = settings.HideRepeatedArticlesInGlobalViews,
        ReadNowFavoriteDays = settings.ReadNowFavoriteDays,
        SpeechEngine = settings.SpeechEngine,
        SpeechVoiceName = settings.SpeechVoiceName,
        EspeakVoiceName = settings.EspeakVoiceName,
        EspeakVariant = settings.EspeakVariant,
        GeminiVoiceName = settings.GeminiVoiceName,
        EspeakPitch = settings.EspeakPitch,
        EspeakInflection = settings.EspeakInflection,
        SpeechRate = settings.SpeechRate,
        SpeechVolume = settings.SpeechVolume,
        StopSpeechWhenLeavingArticle = settings.StopSpeechWhenLeavingArticle,
        ReaderWindowWidth = settings.ReaderWindowWidth,
        ReaderWindowHeight = settings.ReaderWindowHeight,
        ReaderFontSize = settings.ReaderFontSize,
        ReaderWideSpacing = settings.ReaderWideSpacing
    };
}
