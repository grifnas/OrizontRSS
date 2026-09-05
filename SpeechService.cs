namespace CititorRSS.Jaws;

/// <summary>
/// Fațadă comună care păstrează un singur motor vocal Orizont activ.
/// Cititorul de ecran și sintetizatorul aplicației rămân procese și obiecte complet separate.
/// </summary>
public sealed class SpeechService : IDisposable
{
    private ISpeechEngine? _active;
    private string _activeEngineId = SpeechEngineIds.Sapi5;
    private bool _disposed;

    public event Action<string>? StateChanged;
    public bool IsAvailable => !_disposed && _active?.IsAvailable == true;
    public bool IsPaused => !_disposed && _active?.IsPaused == true;
    public bool IsSpeaking => !_disposed && _active?.IsSpeaking == true;
    public string ActiveEngineId => _activeEngineId;

    public bool EnsureAvailable() => !_disposed && _active?.EnsureAvailable() == true;

    public IReadOnlyList<SpeechVoiceChoice> InstalledVoices(string? engineId = null)
    {
        if (_disposed) return [];
        return Activate(engineId ?? _activeEngineId).InstalledVoices();
    }

    public bool Configure(SpeechConfiguration configuration)
    {
        if (_disposed) return false;
        return Activate(configuration.EngineId).Configure(configuration);
    }

    public bool Configure(string? voiceName, int rate, int volume) => Configure(
        new SpeechConfiguration(SpeechEngineIds.Sapi5, voiceName, null, rate, volume));

    public bool Speak(string text) => !_disposed && _active?.Speak(text) == true;
    public bool PauseOrResume() => !_disposed && _active?.PauseOrResume() == true;
    public void Stop(bool reportState = true) { if (!_disposed) _active?.Stop(reportState); }

    private ISpeechEngine Activate(string? engineId)
    {
        var normalized = SpeechEngineIds.Normalize(engineId);
        if (_active is not null && string.Equals(normalized, _activeEngineId, StringComparison.Ordinal)) return _active;

        ReleaseActiveEngine();
        _activeEngineId = normalized;
        _active = normalized switch
        {
            SpeechEngineIds.EspeakNg => new EspeakSpeechEngine(),
            SpeechEngineIds.GeminiTts => new GeminiSpeechEngine(),
            _ => new Sapi5SpeechEngine()
        };
        _active.StateChanged += RelayState;
        return _active;
    }

    private void ReleaseActiveEngine()
    {
        var engine = _active;
        _active = null;
        if (engine is null) return;
        engine.StateChanged -= RelayState;
        engine.Stop(reportState: false);
        engine.Dispose();
    }

    private void RelayState(string state) => StateChanged?.Invoke(state);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ReleaseActiveEngine();
    }
}
