using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

/// <summary>eSpeak NG inclus local; sintetizează PCM, apoi redă WAV prin Windows.</summary>
internal sealed class EspeakSpeechEngine : ISpeechEngine
{
    private static readonly object NativeLock = new();
    private static readonly NativeMethods.SynthCallback SynthCallback = ReceiveSamples;
    private static bool _nativeInitialized;
    private static int _sampleRate;
    private static List<short>? _sampleTarget;
    private static CancellationToken _synthesisCancellation;

    private readonly Dispatcher _dispatcher;
    private MediaPlayer? _player;
    private CancellationTokenSource? _cancellation;
    private string? _wavePath;
    private string _voiceName = "ro";
    private int _rate;
    private int _volume = 100;
    private int _pitch = 50;
    private int _generation;
    private bool _paused;
    private bool _speaking;
    private bool _disposed;

    public event Action<string>? StateChanged;
    public bool IsAvailable => !_disposed && NativeFilesExist() && EnsureNativeInitialized();
    public bool IsPaused => _paused;
    public bool IsSpeaking => _speaking;

    public EspeakSpeechEngine()
    {
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
    }

    public bool EnsureAvailable() => IsAvailable;

    public IReadOnlyList<SpeechVoiceChoice> InstalledVoices()
    {
        if (!EnsureAvailable()) return [];
        lock (NativeLock)
        {
            try
            {
                var list = NativeMethods.espeak_ListVoices(IntPtr.Zero);
                var result = new List<SpeechVoiceChoice>();
                for (var index = 0; ; index++)
                {
                    var voicePointer = Marshal.ReadIntPtr(list, index * IntPtr.Size);
                    if (voicePointer == IntPtr.Zero) break;
                    var voice = Marshal.PtrToStructure<NativeMethods.Voice>(voicePointer);
                    var identifier = Utf8(voice.Identifier);
                    var name = Utf8(voice.Name);
                    var language = PrimaryLanguage(voice.Languages);
                    if (string.IsNullOrWhiteSpace(language) || identifier.StartsWith("mb/", StringComparison.OrdinalIgnoreCase) || identifier.StartsWith("mb\\", StringComparison.OrdinalIgnoreCase)) continue;
                    result.Add(new SpeechVoiceChoice(language, string.IsNullOrWhiteSpace(name) ? language : $"{name} ({language})"));
                }
                return result
                    .GroupBy(voice => voice.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .OrderBy(voice => voice.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }
            catch { return []; }
        }
    }

    public bool Configure(SpeechConfiguration configuration)
    {
        _voiceName = string.IsNullOrWhiteSpace(configuration.EspeakVoiceName) ? "ro" : configuration.EspeakVoiceName;
        _rate = Math.Clamp(configuration.Rate, -10, 10);
        _volume = Math.Clamp(configuration.Volume, 0, 100);
        _pitch = Math.Clamp(configuration.EspeakPitch, 0, 100);
        return EnsureAvailable();
    }

    public bool Speak(string text)
    {
        if (_disposed || string.IsNullOrWhiteSpace(text) || !EnsureAvailable())
        {
            StateChanged?.Invoke(T("Motorul eSpeak NG nu este disponibil."));
            return false;
        }

        Stop(reportState: false);
        var generation = ++_generation;
        _cancellation = new CancellationTokenSource();
        var token = _cancellation.Token;
        _speaking = true;
        StateChanged?.Invoke(T("eSpeak NG pregătește citirea."));

        _ = Task.Run(() => SynthesizeWave(text.Trim(), token), token).ContinueWith(task =>
        {
            _dispatcher.BeginInvoke(() => FinishSynthesis(task, generation));
        }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
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

    private byte[] SynthesizeWave(string text, CancellationToken cancellation)
    {
        lock (NativeLock)
        {
            cancellation.ThrowIfCancellationRequested();
            var samples = new List<short>(Math.Max(4096, text.Length * 100));
            _sampleTarget = samples;
            _synthesisCancellation = cancellation;
            try
            {
                NativeMethods.espeak_SetSynthCallback(SynthCallback);
                if (NativeMethods.espeak_SetVoiceByName(_voiceName) != 0 && NativeMethods.espeak_SetVoiceByName("ro") != 0)
                    throw new InvalidOperationException(T("Vocea eSpeak selectată nu este disponibilă."));
                NativeMethods.espeak_SetParameter(1, Math.Clamp(175 + _rate * 15, 80, 450), 0);
                NativeMethods.espeak_SetParameter(2, _volume, 0);
                NativeMethods.espeak_SetParameter(3, _pitch, 0);

                var utf8 = Encoding.UTF8.GetBytes(text + "\0");
                var result = NativeMethods.espeak_Synth(utf8, (nuint)utf8.Length, 0, 1, 0, 1u | 0x1000u, IntPtr.Zero, IntPtr.Zero);
                if (result != 0) throw new InvalidOperationException(F("eSpeak NG a respins textul cu eroarea {0}.", result));
                NativeMethods.espeak_Synchronize();
                cancellation.ThrowIfCancellationRequested();
                if (samples.Count == 0) throw new InvalidOperationException(T("eSpeak NG nu a produs sunet."));
                return CreateWave(samples, _sampleRate);
            }
            finally
            {
                _sampleTarget = null;
                _synthesisCancellation = default;
            }
        }
    }

    private void FinishSynthesis(Task<byte[]> task, int generation)
    {
        if (_disposed || generation != _generation || task.IsCanceled) return;
        if (task.IsFaulted)
        {
            _speaking = false;
            var message = task.Exception?.GetBaseException().Message ?? T("Eroare necunoscută eSpeak NG.");
            StateChanged?.Invoke(F("Citirea cu eSpeak NG nu a putut porni: {0}", message));
            return;
        }

        try
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Orizont RSS", "speech-temp");
            Directory.CreateDirectory(directory);
            _wavePath = Path.Combine(directory, $"espeak-{Guid.NewGuid():N}.wav");
            File.WriteAllBytes(_wavePath, task.Result);
            _player = new MediaPlayer();
            _player.MediaEnded += PlayerEnded;
            _player.MediaFailed += PlayerFailed;
            // Volumul a fost aplicat deja de eSpeak la generarea eșantioanelor.
            _player.Volume = 1d;
            _player.Open(new Uri(_wavePath, UriKind.Absolute));
            _player.Play();
            _paused = false;
            StateChanged?.Invoke(T("Citire vocală pornită cu eSpeak NG."));
        }
        catch (Exception exception)
        {
            _speaking = false;
            ClosePlayer();
            DeleteWave();
            StateChanged?.Invoke(F("Sunetul eSpeak NG nu a putut fi redat: {0}", exception.Message));
        }
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
        StateChanged?.Invoke(F("Redarea eSpeak NG s-a oprit cu eroare: {0}", e.ErrorException.Message));
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

    private static int ReceiveSamples(IntPtr wave, int sampleCount, IntPtr events)
    {
        if (_synthesisCancellation.IsCancellationRequested) return 1;
        if (wave == IntPtr.Zero || sampleCount <= 0 || _sampleTarget is null) return 0;
        var buffer = new short[sampleCount];
        Marshal.Copy(wave, buffer, 0, sampleCount);
        _sampleTarget.AddRange(buffer);
        return 0;
    }

    private static bool EnsureNativeInitialized()
    {
        lock (NativeLock)
        {
            if (_nativeInitialized) return true;
            try
            {
                _sampleRate = NativeMethods.espeak_Initialize(1, 0, EngineDirectory(), 0);
                _nativeInitialized = _sampleRate > 0;
                if (_nativeInitialized) NativeMethods.espeak_SetSynthCallback(SynthCallback);
                return _nativeInitialized;
            }
            catch { return false; }
        }
    }

    private static bool NativeFilesExist() =>
        File.Exists(Path.Combine(AppContext.BaseDirectory, "libespeak-ng.dll")) &&
        Directory.Exists(Path.Combine(EngineDirectory(), "espeak-ng-data"));

    private static string EngineDirectory() => Path.Combine(AppContext.BaseDirectory, "SpeechEngines", "eSpeakNG");
    private static string Utf8(IntPtr pointer) => pointer == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUTF8(pointer) ?? string.Empty;
    private static string PrimaryLanguage(IntPtr pointer) => pointer == IntPtr.Zero || Marshal.ReadByte(pointer) == 0
        ? string.Empty
        : Marshal.PtrToStringUTF8(IntPtr.Add(pointer, 1)) ?? string.Empty;

    private static byte[] CreateWave(IReadOnlyCollection<short> samples, int sampleRate)
    {
        using var stream = new MemoryStream(44 + samples.Count * sizeof(short));
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        var dataLength = samples.Count * sizeof(short);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataLength);
        writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * sizeof(short));
        writer.Write((short)sizeof(short));
        writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataLength);
        foreach (var sample in samples) writer.Write(sample);
        writer.Flush();
        return stream.ToArray();
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop(reportState: false);
        _disposed = true;
    }

    private static string T(string source) => UiText.Translate(source);
    private static string F(string source, params object?[] arguments) => UiText.Format(source, arguments);

    private static class NativeMethods
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int SynthCallback(IntPtr wave, int sampleCount, IntPtr events);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Voice
        {
            internal IntPtr Name;
            internal IntPtr Languages;
            internal IntPtr Identifier;
            internal byte Gender;
            internal byte Age;
            internal byte Variant;
            internal byte Reserved;
            internal int Score;
            internal IntPtr Spare;
        }

        [DllImport("libespeak-ng.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int espeak_Initialize(int output, int bufferLength, string path, int options);

        [DllImport("libespeak-ng.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void espeak_SetSynthCallback(SynthCallback callback);

        [DllImport("libespeak-ng.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr espeak_ListVoices(IntPtr voiceSpec);

        [DllImport("libespeak-ng.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int espeak_SetVoiceByName(string name);

        [DllImport("libespeak-ng.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int espeak_SetParameter(int parameter, int value, int relative);

        [DllImport("libespeak-ng.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int espeak_Synth(byte[] text, nuint size, uint position, int positionType, uint endPosition, uint flags, IntPtr uniqueIdentifier, IntPtr userData);

        [DllImport("libespeak-ng.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int espeak_Synchronize();
    }
}
