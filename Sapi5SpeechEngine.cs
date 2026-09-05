using System.Runtime.InteropServices;
using System.Windows.Threading;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

/// <summary>Local, optional speech through the SAPI5 voices registered in Windows.</summary>
internal sealed class Sapi5SpeechEngine : ISpeechEngine
{
    private const int SpeakAsync = 1;
    private const int PurgeBeforeSpeak = 2;
    private dynamic? _voice;
    private readonly DispatcherTimer _stateTimer;
    private string? _voiceName;
    private int _rate;
    private int _volume = 100;
    private bool _paused;
    private bool _speaking;
    private bool _disposed;

    public event Action<string>? StateChanged;
    public bool IsAvailable => !_disposed && _voice is not null;
    public bool IsPaused => _paused;
    public bool IsSpeaking => _speaking;

    public Sapi5SpeechEngine()
    {
        _stateTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _stateTimer.Tick += CheckState;
        CreateVoice();
    }

    /// <summary>Verifică obiectul COM și îl recreează dacă Windows l-a invalidat după repaus.</summary>
    public bool EnsureAvailable()
    {
        if (_disposed) return false;
        if (_voice is not null)
        {
            try
            {
                var voices = _voice.GetVoices(string.Empty, string.Empty);
                if ((int)voices.Count > 0) return true;
                ResetLostVoice();
            }
            catch
            {
                ResetLostVoice();
            }
        }
        return RecreateVoice();
    }

    public IReadOnlyList<SpeechVoiceChoice> InstalledVoices()
    {
        if (!EnsureAvailable()) return [];
        try { return ReadInstalledVoices(); }
        catch
        {
            if (!RecreateVoice()) return [];
            try { return ReadInstalledVoices(); } catch { return []; }
        }
    }

    public bool Configure(SpeechConfiguration configuration)
    {
        _voiceName = configuration.SapiVoiceName;
        _rate = Math.Clamp(configuration.Rate, -10, 10);
        _volume = Math.Clamp(configuration.Volume, 0, 100);
        if (!EnsureAvailable()) return false;
        try
        {
            ApplyConfiguration();
            return true;
        }
        catch
        {
            if (!RecreateVoice()) return false;
            try { ApplyConfiguration(); return true; } catch { return false; }
        }
    }

    public bool Speak(string text)
    {
        if (_disposed || string.IsNullOrWhiteSpace(text)) return false;

        // O instanță proaspătă evită obiectele SAPI5 rămase invalide după repaus sau suspendarea Windows.
        if (!RecreateVoice())
        {
            StateChanged?.Invoke(T("Citirea vocală nu a putut porni deoarece motorul SAPI5 nu a putut fi inițializat."));
            return false;
        }

        try
        {
            StartSpeech(text);
            return true;
        }
        catch
        {
            // Unele sintetizatoare eșuează la prima inițializare după revenirea din repaus.
            if (RecreateVoice())
            {
                try
                {
                    StartSpeech(text);
                    StateChanged?.Invoke(T("Motorul SAPI5 a fost reinițializat automat. Citire vocală pornită."));
                    return true;
                }
                catch { }
            }
            _speaking = false;
            _paused = false;
            StateChanged?.Invoke(T("Citirea vocală nu a putut porni nici după reinițializarea motorului SAPI5. Încearcă din nou sau verifică vocea în Setări."));
            return false;
        }
    }

    public bool PauseOrResume()
    {
        if (_voice is null || !_speaking) return false;
        try
        {
            if (_paused)
            {
                _voice.Resume();
                _paused = false;
                StateChanged?.Invoke(T("Citire vocală continuată."));
            }
            else
            {
                _voice.Pause();
                _paused = true;
                StateChanged?.Invoke(T("Citire vocală întreruptă."));
            }
            return true;
        }
        catch
        {
            ResetLostVoice();
            StateChanged?.Invoke(T("Motorul SAPI5 nu mai răspunde. Pornește din nou citirea cu Ctrl+Alt+V; motorul va fi reinițializat automat."));
            return false;
        }
    }

    public void Stop(bool reportState = true)
    {
        var hadSpeech = _speaking || _paused;
        StopCore();
        if (reportState && hadSpeech) StateChanged?.Invoke(T("Citire vocală oprită."));
    }

    private void StartSpeech(string text)
    {
        if (_voice is null) throw new InvalidOperationException(T("Motorul SAPI5 nu este disponibil."));
        _voice.Speak(text.Trim(), SpeakAsync);
        _paused = false;
        _speaking = true;
        _stateTimer.Start();
        StateChanged?.Invoke(T("Citire vocală pornită."));
    }

    private bool RecreateVoice()
    {
        if (_disposed) return false;
        StopCore();
        ReleaseVoice();
        if (!CreateVoice()) return false;
        try
        {
            ApplyConfiguration();
            return true;
        }
        catch
        {
            ReleaseVoice();
            return false;
        }
    }

    private bool CreateVoice()
    {
        if (_disposed) return false;
        try
        {
            var sapiType = Type.GetTypeFromProgID("SAPI.SpVoice", throwOnError: false);
            _voice = sapiType is null ? null : Activator.CreateInstance(sapiType);
            return _voice is not null;
        }
        catch
        {
            _voice = null;
            return false;
        }
    }

    private void ApplyConfiguration()
    {
        if (_voice is null) throw new InvalidOperationException(T("Motorul SAPI5 nu este disponibil."));
        _voice.Rate = _rate;
        _voice.Volume = _volume;
        if (string.IsNullOrWhiteSpace(_voiceName)) return;
        var voices = _voice.GetVoices(string.Empty, string.Empty);
        for (var index = 0; index < (int)voices.Count; index++)
        {
            var token = voices.Item(index);
            if (!string.Equals((string)token.GetDescription(0), _voiceName, StringComparison.CurrentCultureIgnoreCase)) continue;
            _voice.Voice = token;
            break;
        }
    }

    private IReadOnlyList<SpeechVoiceChoice> ReadInstalledVoices()
    {
        if (_voice is null) return [];
        var voices = _voice.GetVoices(string.Empty, string.Empty);
        var result = new List<SpeechVoiceChoice>();
        for (var index = 0; index < (int)voices.Count; index++)
        {
            var name = (string)voices.Item(index).GetDescription(0);
            result.Add(new SpeechVoiceChoice(name, name));
        }
        return result;
    }

    private void CheckState(object? sender, EventArgs e)
    {
        if (_voice is null || !_speaking || _paused) return;
        try
        {
            // SAPI RunningState: 1 = terminat, 2 = vorbește.
            if ((int)_voice.Status.RunningState != 1) return;
            _stateTimer.Stop();
            _speaking = false;
            StateChanged?.Invoke(T("Citirea vocală s-a încheiat."));
        }
        catch
        {
            ResetLostVoice();
            StateChanged?.Invoke(T("Legătura cu motorul SAPI5 s-a întrerupt. Următoarea citire îl va reinițializa automat."));
        }
    }

    private void StopCore()
    {
        if (_voice is not null && (_speaking || _paused))
        {
            try { _voice.Speak(string.Empty, PurgeBeforeSpeak); } catch { }
        }
        _stateTimer.Stop();
        _speaking = false;
        _paused = false;
    }

    private void ResetLostVoice()
    {
        _stateTimer.Stop();
        _speaking = false;
        _paused = false;
        ReleaseVoice();
    }

    private void ReleaseVoice()
    {
        var voice = _voice;
        _voice = null;
        if (voice is null || !Marshal.IsComObject(voice)) return;
        try { Marshal.FinalReleaseComObject(voice); } catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        StopCore();
        ReleaseVoice();
        _disposed = true;
    }
    private static string T(string source) => UiText.Translate(source);
}
