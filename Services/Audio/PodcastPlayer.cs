using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SoundTouch;

namespace CititorRSS.Jaws;

/// <summary>
/// Redă episoade de podcast de pe internet. Sunetul trece prin SoundTouch, deci viteza (0,5–2) și tonul
/// (0,5–1,5) se schimbă independent unul de celălalt.
/// </summary>
internal sealed class PodcastPlayer : IDisposable
{
    public const double MinimumSpeed = 0.5, MaximumSpeed = 2.0;
    public const double MinimumPitch = 0.5, MaximumPitch = 1.5;

    private readonly object _gate = new();
    private MediaFoundationReader? _reader;
    private ISampleProvider? _source;
    private StretchingSampleProvider? _stretch;
    private VolumeSampleProvider? _volume;
    private WaveOutEvent? _output;
    private string? _url;

    public double Speed { get; private set; } = 1.0;
    public double Pitch { get; private set; } = 1.0;
    /// <summary>Volumul podcastului, independent de volumul sistemului; 0–100.</summary>
    public int VolumePercent { get; private set; } = 100;
    public bool IsOpen => _reader is not null;
    public bool IsPlaying => _output?.PlaybackState == PlaybackState.Playing;
    public TimeSpan Position => _reader?.CurrentTime ?? TimeSpan.Zero;
    public TimeSpan Duration => _reader?.TotalTime ?? TimeSpan.Zero;
    public string? Url => _url;

    /// <summary>Deschide adresa și pornește redarea. Redarea se face în fundal, fără să blocheze interfața.</summary>
    public void Play(string url)
    {
        lock (_gate)
        {
            if (!string.Equals(_url, url, StringComparison.Ordinal)) Open(url);
            _output?.Play();
        }
    }

    public void TogglePause()
    {
        lock (_gate)
        {
            if (_output is null) return;
            if (_output.PlaybackState == PlaybackState.Playing) _output.Pause(); else _output.Play();
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            _output?.Stop();
        }
    }

    /// <summary>Mută poziția cu atâtea secunde; negativ înapoi, pozitiv înainte.</summary>
    public void Seek(double seconds)
    {
        lock (_gate)
        {
            if (_reader is null) return;
            var target = _reader.CurrentTime + TimeSpan.FromSeconds(seconds);
            if (target < TimeSpan.Zero) target = TimeSpan.Zero;
            if (target > _reader.TotalTime) target = _reader.TotalTime;
            _reader.CurrentTime = target;
            _stretch?.Clear();
        }
    }

    public void SetSpeed(double speed)
    {
        Speed = Math.Clamp(Math.Round(speed, 2), MinimumSpeed, MaximumSpeed);
        lock (_gate) { if (_stretch is not null) _stretch.Tempo = Speed; }
    }

    public void SetPitch(double pitch)
    {
        Pitch = Math.Clamp(Math.Round(pitch, 2), MinimumPitch, MaximumPitch);
        lock (_gate) { if (_stretch is not null) _stretch.PitchRatio = Pitch; }
    }

    /// <summary>Volumul se aplică în aplicație, după SoundTouch; volumul Windows rămâne neschimbat.</summary>
    public void SetVolumePercent(int percent)
    {
        VolumePercent = Math.Clamp(percent, 0, 100);
        lock (_gate) { if (_volume is not null) _volume.Volume = VolumePercent / 100f; }
    }

    private void Open(string url)
    {
        CloseCurrent();
        _reader = new MediaFoundationReader(url);
        _source = _reader.ToSampleProvider();
        _stretch = new StretchingSampleProvider(_source, _source.WaveFormat.SampleRate, _source.WaveFormat.Channels)
        {
            Tempo = Speed,
            PitchRatio = Pitch
        };
        _volume = new VolumeSampleProvider(_stretch) { Volume = VolumePercent / 100f };
        _output = new WaveOutEvent();
        _output.Init(_volume.ToWaveProvider());
        _url = url;
    }

    private void CloseCurrent()
    {
        _output?.Stop();
        _output?.Dispose();
        _output = null;
        _reader?.Dispose();
        _reader = null;
        _source = null;
        _stretch = null;
        _volume = null;
        _url = null;
    }

    public void Dispose()
    {
        lock (_gate) CloseCurrent();
    }

    /// <summary>Trece sunetul prin SoundTouch: viteza (tempo) și tonul (raport de frecvență) se aplică separat.</summary>
    private sealed class StretchingSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly SoundTouchProcessor _processor = new();
        private readonly float[] _input;
        private readonly int _channels;
        private bool _endOfSource;

        public StretchingSampleProvider(ISampleProvider source, int sampleRate, int channels)
        {
            _source = source;
            _channels = channels;
            _input = new float[sampleRate / 10 * channels];
            _processor.SampleRate = sampleRate;
            _processor.Channels = channels;
        }

        public WaveFormat WaveFormat => _source.WaveFormat;

        public double Tempo { get => _processor.Tempo; set => _processor.Tempo = value; }

        public double PitchRatio { get => _processor.Pitch; set => _processor.Pitch = value; }

        public void Clear()
        {
            _processor.Clear();
            _endOfSource = false;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var framesWanted = count / _channels;
            var frames = 0;
            while (frames < framesWanted)
            {
                var received = _processor.ReceiveSamples(buffer.AsSpan(offset + frames * _channels, (framesWanted - frames) * _channels), framesWanted - frames);
                frames += received;
                if (received > 0) continue;
                if (_endOfSource) break;
                var read = _source.Read(_input, 0, _input.Length);
                if (read == 0)
                {
                    _endOfSource = true;
                    _processor.Flush();
                    continue;
                }
                _processor.PutSamples(_input.AsSpan(0, read), read / _channels);
            }
            return frames * _channels;
        }
    }
}
