namespace CititorRSS.Jaws;

public static class SpeechEngineIds
{
    public const string Sapi5 = "Sapi5";
    public const string EspeakNg = "EspeakNg";
    public const string GeminiTts = "GeminiTts";

    public static string Normalize(string? value)
    {
        if (string.Equals(value, EspeakNg, StringComparison.OrdinalIgnoreCase)) return EspeakNg;
        if (string.Equals(value, GeminiTts, StringComparison.OrdinalIgnoreCase)) return GeminiTts;
        return Sapi5;
    }
}

public sealed record SpeechConfiguration(
    string EngineId,
    string? SapiVoiceName,
    string? EspeakVoiceName,
    int Rate,
    int Volume,
    int EspeakPitch = 50,
    string? GeminiVoiceName = null,
    string? GeminiApiKey = null);

public sealed record SpeechVoiceChoice(string Id, string DisplayName)
{
    public override string ToString() => DisplayName;
}

internal interface ISpeechEngine : IDisposable
{
    event Action<string>? StateChanged;
    bool IsAvailable { get; }
    bool IsPaused { get; }
    bool IsSpeaking { get; }
    bool EnsureAvailable();
    IReadOnlyList<SpeechVoiceChoice> InstalledVoices();
    bool Configure(SpeechConfiguration configuration);
    bool Speak(string text);
    bool PauseOrResume();
    void Stop(bool reportState = true);
}
