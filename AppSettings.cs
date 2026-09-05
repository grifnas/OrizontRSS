namespace CititorRSS.Jaws;

public sealed class AppSettings
{
    public string? UiLanguage { get; set; }
    public bool AutoCleanupEnabled { get; set; }
    public int RetentionDays { get; set; } = 90;
    public bool UpdateAtStartup { get; set; }
    public bool SoundAlertsEnabled { get; set; } = true;
    public bool SoundAlertOnSuccess { get; set; } = true;
    public bool SoundAlertOnNewArticles { get; set; } = true;
    public bool SoundAlertOnErrors { get; set; } = true;
    public string AiInstructions { get; set; } = "Răspunde în limba interfeței. Fii clar, concis și semnalează incertitudinile.";
    public bool GeminiEnabled { get; set; }
    public string? EncryptedGeminiKey { get; set; }
    public bool DeepLEnabled { get; set; }
    public string? EncryptedDeepLKey { get; set; }
    public string LastFolder { get; set; } = "Toate folderele";
    public Guid? LastFeedId { get; set; }
    public string? LastArticleId { get; set; }
    public string? LastArticleLink { get; set; }
    public string LastArticleFilter { get; set; } = "Toate";
    public string LastTimeFilter { get; set; } = "Oricând";
    public string LastTagFilter { get; set; } = "Toate etichetele";
    public string LastView { get; set; } = "Folder";
    public string LastPanel { get; set; } = "Articles";
    public bool LastAttentionFilter { get; set; }
    public bool HideRepeatedArticlesInGlobalViews { get; set; } = true;
    public int ReadNowFavoriteDays { get; set; } = 7;
    public string SpeechEngine { get; set; } = SpeechEngineIds.Sapi5;
    public string? SpeechVoiceName { get; set; }
    public string EspeakVoiceName { get; set; } = "ro";
    public string GeminiVoiceName { get; set; } = "Charon";
    public int EspeakPitch { get; set; } = 50;
    public int SpeechRate { get; set; }
    public int SpeechVolume { get; set; } = 100;
    public bool StopSpeechWhenLeavingArticle { get; set; } = true;
    public double ReaderWindowWidth { get; set; } = 900;
    public double ReaderWindowHeight { get; set; } = 700;
    public double ReaderFontSize { get; set; } = 18;
    public bool ReaderWideSpacing { get; set; }
}
