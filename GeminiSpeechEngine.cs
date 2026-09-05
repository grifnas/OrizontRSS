using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

/// <summary>Generează voce online prin Gemini TTS și redă local sunetul PCM primit.</summary>
internal sealed class GeminiSpeechEngine : ISpeechEngine
{
    private const string Model = "gemini-3.1-flash-tts-preview";
    private const int SampleRate = 24000;
    private const int MaximumTextLength = 100000;
    private static readonly IReadOnlyList<SpeechVoiceChoice> Voices =
    [
        new("Zephyr", "Zephyr"), new("Puck", "Puck"), new("Charon", "Charon"), new("Kore", "Kore"),
        new("Fenrir", "Fenrir"), new("Leda", "Leda"), new("Orus", "Orus"), new("Aoede", "Aoede"),
        new("Callirrhoe", "Callirrhoe"), new("Autonoe", "Autonoe"), new("Enceladus", "Enceladus"),
        new("Iapetus", "Iapetus"), new("Umbriel", "Umbriel"), new("Algieba", "Algieba"),
        new("Despina", "Despina"), new("Erinome", "Erinome"), new("Algenib", "Algenib"),
        new("Rasalgethi", "Rasalgethi"), new("Laomedeia", "Laomedeia"), new("Achernar", "Achernar"),
        new("Alnilam", "Alnilam"), new("Schedar", "Schedar"), new("Gacrux", "Gacrux"),
        new("Pulcherrima", "Pulcherrima"), new("Achird", "Achird"), new("Zubenelgenubi", "Zubenelgenubi"),
        new("Vindemiatrix", "Vindemiatrix"), new("Sadachbia", "Sadachbia"),
        new("Sadaltager", "Sadaltager"), new("Sulafat", "Sulafat")
    ];

    private readonly Dispatcher _dispatcher;
    private readonly HttpClient _client;
    private MediaPlayer? _player;
    private CancellationTokenSource? _cancellation;
    private string? _wavePath;
    private string _voiceName = "Charon";
    private string? _apiKey;
    private int _rate;
    private int _volume = 100;
    private int _generation;
    private bool _paused;
    private bool _speaking;
    private bool _disposed;

    public event Action<string>? StateChanged;
    public bool IsAvailable => !_disposed && !string.IsNullOrWhiteSpace(_apiKey);
    public bool IsPaused => _paused;
    public bool IsSpeaking => _speaking;

    public GeminiSpeechEngine()
    {
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        _client = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
    }

    public bool EnsureAvailable() => IsAvailable;
    public IReadOnlyList<SpeechVoiceChoice> InstalledVoices() => Voices;

    public bool Configure(SpeechConfiguration configuration)
    {
        _voiceName = Voices.Any(voice => string.Equals(voice.Id, configuration.GeminiVoiceName, StringComparison.OrdinalIgnoreCase))
            ? configuration.GeminiVoiceName!
            : "Charon";
        _apiKey = string.IsNullOrWhiteSpace(configuration.GeminiApiKey) ? null : configuration.GeminiApiKey.Trim();
        _rate = Math.Clamp(configuration.Rate, -10, 10);
        _volume = Math.Clamp(configuration.Volume, 0, 100);
        return EnsureAvailable();
    }

    public bool Speak(string text)
    {
        if (_disposed || string.IsNullOrWhiteSpace(text)) return false;
        if (!EnsureAvailable())
        {
            StateChanged?.Invoke(T("Gemini TTS necesită o cheie API Gemini salvată în setările Inteligență artificială."));
            return false;
        }
        if (text.Length > MaximumTextLength)
        {
            StateChanged?.Invoke(F("Textul este prea lung pentru citirea Gemini TTS. Limita locală este de {0} caractere.", MaximumTextLength));
            return false;
        }

        Stop(reportState: false);
        var generation = ++_generation;
        _cancellation = new CancellationTokenSource();
        var token = _cancellation.Token;
        _speaking = true;
        StateChanged?.Invoke(T("Gemini pregătește vocea online. Procesul poate dura câteva secunde."));
        _ = GenerateWaveAsync(text.Trim(), token).ContinueWith(task =>
            _dispatcher.BeginInvoke(() => FinishGeneration(task, generation)),
            CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
        return true;
    }

    public bool PauseOrResume()
    {
        if (_player is null || !_speaking) return false;
        if (_paused)
        {
            _player.Play();
            _paused = false;
            StateChanged?.Invoke(T("Citire vocală continuată."));
        }
        else
        {
            _player.Pause();
            _paused = true;
            StateChanged?.Invoke(T("Citire vocală întreruptă."));
        }
        return true;
    }

    public void Stop(bool reportState = true)
    {
        var hadSpeech = _speaking || _paused;
        ++_generation;
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = null;
        ClosePlayer();
        _speaking = false;
        _paused = false;
        DeleteWave();
        if (reportState && hadSpeech) StateChanged?.Invoke(T("Citire vocală oprită."));
    }

    private async Task<byte[]> GenerateWaveAsync(string text, CancellationToken cancellation)
    {
        var request = new
        {
            contents = new[] { new { parts = new[] { new { text = BuildPrompt(text) } } } },
            generationConfig = new
            {
                responseModalities = new[] { "AUDIO" },
                speechConfig = new
                {
                    voiceConfig = new { prebuiltVoiceConfig = new { voiceName = _voiceName } }
                }
            }
        };
        using var message = new HttpRequestMessage(HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent");
        message.Headers.Add("x-goog-api-key", _apiKey);
        message.Content = JsonContent.Create(request);
        using var response = await _client.SendAsync(message, cancellation);
        var body = await response.Content.ReadAsStringAsync(cancellation);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException(DescribeError(response.StatusCode, body));

        using var document = JsonDocument.Parse(body);
        if (!TryFindAudio(document.RootElement, out var encodedAudio))
            throw new InvalidOperationException(T("Gemini nu a returnat sunet. Textul poate fi prea lung sau filtrat de regulile serviciului."));
        cancellation.ThrowIfCancellationRequested();
        byte[] pcm;
        try { pcm = Convert.FromBase64String(encodedAudio); }
        catch (FormatException) { throw new InvalidOperationException(T("Gemini a returnat date audio nevalide.")); }
        if (pcm.Length == 0 || pcm.Length % 2 != 0)
            throw new InvalidOperationException(T("Gemini a returnat un flux audio incomplet."));
        return CreateWave(pcm, SampleRate);
    }

    private string BuildPrompt(string text)
    {
        var pace = _rate switch
        {
            <= -6 => "foarte lent",
            <= -2 => "lent",
            >= 6 => "foarte rapid",
            >= 2 => "rapid",
            _ => "normal"
        };
        return $"Citește cu fidelitate textul de mai jos, fără să adaugi, să omiți sau să reformulezi nimic. " +
               $"Folosește limba textului, pronunție clară, ton natural de lectură și ritm {pace}.\n\n{text}";
    }

    private void FinishGeneration(Task<byte[]> task, int generation)
    {
        if (_disposed || generation != _generation) return;
        _cancellation?.Dispose();
        _cancellation = null;
        if (task.IsCanceled)
        {
            _speaking = false;
            StateChanged?.Invoke(T("Serviciul Gemini TTS nu a răspuns în timpul permis."));
            return;
        }
        if (task.IsFaulted)
        {
            _speaking = false;
            var cause = task.Exception?.GetBaseException();
            var message = cause switch
            {
                HttpRequestException => T("Nu s-a putut ajunge la Gemini TTS. Verifică legătura la internet, firewall-ul sau proxy-ul."),
                _ => cause?.Message ?? T("Eroare necunoscută Gemini TTS.")
            };
            StateChanged?.Invoke(F("Citirea cu Gemini TTS nu a putut porni: {0}", message));
            return;
        }

        try
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Orizont RSS", "speech-temp");
            Directory.CreateDirectory(directory);
            _wavePath = Path.Combine(directory, $"gemini-{Guid.NewGuid():N}.wav");
            File.WriteAllBytes(_wavePath, task.Result);
            _player = new MediaPlayer();
            _player.MediaEnded += PlayerEnded;
            _player.MediaFailed += PlayerFailed;
            _player.Volume = _volume / 100d;
            _player.Open(new Uri(_wavePath, UriKind.Absolute));
            _player.Play();
            _paused = false;
            StateChanged?.Invoke(F("Citire vocală pornită cu Gemini, vocea {0}.", _voiceName));
        }
        catch (Exception exception)
        {
            _speaking = false;
            ClosePlayer();
            DeleteWave();
            StateChanged?.Invoke(F("Sunetul Gemini nu a putut fi redat: {0}", exception.Message));
        }
    }

    private static bool TryFindAudio(JsonElement root, out string audio)
    {
        audio = string.Empty;
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0 ||
            !candidates[0].TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts)) return false;
        foreach (var part in parts.EnumerateArray())
        {
            if (!part.TryGetProperty("inlineData", out var inlineData) ||
                !inlineData.TryGetProperty("data", out var data)) continue;
            audio = data.GetString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(audio)) return true;
        }
        return false;
    }

    private static string DescribeError(HttpStatusCode status, string body)
    {
        var detail = body.Length > 300 ? body[..300] : body;
        if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return T("Cheia API Gemini nu a fost acceptată sau nu are permisiune pentru Gemini TTS.");
        if ((int)status == 429)
            return T("Gemini TTS a limitat solicitarea sau a fost atinsă cota ori limita de facturare a contului.");
        return F("Gemini TTS a răspuns cu eroarea {0}. Detalii: {1}", (int)status, detail);
    }

    private void PlayerEnded(object? sender, EventArgs e)
    {
        _speaking = false;
        _paused = false;
        ClosePlayer();
        DeleteWave();
        StateChanged?.Invoke(T("Citirea vocală s-a încheiat."));
    }

    private void PlayerFailed(object? sender, ExceptionEventArgs e)
    {
        _speaking = false;
        _paused = false;
        ClosePlayer();
        DeleteWave();
        StateChanged?.Invoke(F("Redarea Gemini TTS s-a oprit cu eroare: {0}", e.ErrorException.Message));
    }

    private void ClosePlayer()
    {
        if (_player is null) return;
        try { _player.Stop(); } catch { }
        _player.MediaEnded -= PlayerEnded;
        _player.MediaFailed -= PlayerFailed;
        _player.Close();
        _player = null;
    }

    private void DeleteWave()
    {
        var path = _wavePath;
        _wavePath = null;
        if (string.IsNullOrWhiteSpace(path)) return;
        try { File.Delete(path); } catch { }
    }

    private static byte[] CreateWave(byte[] pcm, int sampleRate)
    {
        using var stream = new MemoryStream(44 + pcm.Length);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + pcm.Length);
        writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * sizeof(short));
        writer.Write((short)sizeof(short));
        writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(pcm.Length);
        writer.Write(pcm);
        writer.Flush();
        return stream.ToArray();
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop(reportState: false);
        _client.Dispose();
        _apiKey = null;
        _disposed = true;
    }

    private static string T(string source) => UiText.Translate(source);
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);
}
