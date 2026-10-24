using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

// Podcasturi în Cititorul Orizont: taste în modul text, ca la un casetofon (Z X C V B), plus S, P și D.
public partial class ArticleReaderWindow
{
    private const double BigJumpSeconds = 30, SmallJumpSeconds = 5;
    private const int PodcastVolumeStep = 5;
    private static readonly HttpClient PodcastDownloadClient = new() { Timeout = TimeSpan.FromMinutes(30) };
    private PodcastPlayer? _podcast;
    private bool _podcastBusy;

    private bool HasPodcast => !string.IsNullOrWhiteSpace(_article.AudioUrl);

    /// <summary>Tratează tastele podcastului în modul text. Întoarce true dacă tasta a fost folosită.</summary>
    private bool HandlePodcastKey(KeyEventArgs e)
    {
        if (_isWebViewActive || !HasPodcast) return false;
        var key = ContextMenuKeyboard.Normalize(e.Key, e.SystemKey);
        if (Keyboard.Modifiers == ModifierKeys.Alt && (key == Key.Up || key == Key.Down))
        {
            PodcastChangeVolume(key == Key.Up ? PodcastVolumeStep : -PodcastVolumeStep);
            return true;
        }
        if (Keyboard.Modifiers != ModifierKeys.None) return false;
        switch (key)
        {
            case Key.Z: PodcastSeek(-BigJumpSeconds); return true;
            case Key.X: PodcastSeek(-SmallJumpSeconds); return true;
            case Key.C: _ = PodcastTogglePlaybackAsync(); return true;
            case Key.V: PodcastSeek(SmallJumpSeconds); return true;
            case Key.B: PodcastSeek(BigJumpSeconds); return true;
            case Key.S: PodcastChooseSpeed(); return true;
            case Key.P: PodcastChoosePitch(); return true;
            case Key.D: _ = PodcastDownloadAsync(); return true;
            default: return false;
        }
    }

    public void PodcastPlayFromMenu() => _ = PodcastTogglePlaybackAsync();

    private void PodcastPlay_Click(object sender, RoutedEventArgs e) => PodcastPlayFromMenu();
    private void PodcastBack30_Click(object sender, RoutedEventArgs e) => PodcastSeek(-BigJumpSeconds);
    private void PodcastBack5_Click(object sender, RoutedEventArgs e) => PodcastSeek(-SmallJumpSeconds);
    private void PodcastForward5_Click(object sender, RoutedEventArgs e) => PodcastSeek(SmallJumpSeconds);
    private void PodcastForward30_Click(object sender, RoutedEventArgs e) => PodcastSeek(BigJumpSeconds);
    private void PodcastSpeed_Click(object sender, RoutedEventArgs e) => PodcastChooseSpeed();
    private void PodcastPitch_Click(object sender, RoutedEventArgs e) => PodcastChoosePitch();
    private void PodcastDownload_Click(object sender, RoutedEventArgs e) => _ = PodcastDownloadAsync();

    private async Task PodcastTogglePlaybackAsync()
    {
        if (!HasPodcast) { SetStatus(T("Articolul nu are un podcast.")); return; }
        if (_podcastBusy) return;
        var player = EnsurePodcastPlayer();
        try
        {
            if (!player.IsOpen || !string.Equals(player.Url, _article.AudioUrl, StringComparison.Ordinal))
            {
                _podcastBusy = true;
                SetStatus(T("Se pornește podcastul."));
                var url = _article.AudioUrl!;
                await Task.Run(() => player.Play(url));
                SetStatus(F("Redare podcast: {0} din {1}.", PodcastClock(player.Position), PodcastClock(player.Duration)));
                return;
            }
            player.TogglePause();
            SetStatus(player.IsPlaying
                ? F("Redare podcast: {0} din {1}.", PodcastClock(player.Position), PodcastClock(player.Duration))
                : F("Podcast pus pe pauză: {0}.", PodcastClock(player.Position)));
        }
        catch (Exception exception)
        {
            SetStatus(F("Podcastul nu a putut fi redat: {0}", exception.Message));
        }
        finally { _podcastBusy = false; }
    }

    private void PodcastSeek(double seconds)
    {
        if (!HasPodcast) { SetStatus(T("Articolul nu are un podcast.")); return; }
        var player = _podcast;
        if (player is null || !player.IsOpen) { SetStatus(T("Nu există un podcast pornit. Apasă C pentru redare.")); return; }
        player.Seek(seconds);
        SetStatus(F("Salt la {0}.", PodcastClock(player.Position)));
    }

    /// <summary>Alt+săgeată sus/jos schimbă volumul podcastului cu 5%, independent de volumul Windows.</summary>
    private void PodcastChangeVolume(int change)
    {
        var player = EnsurePodcastPlayer();
        player.SetVolumePercent(player.VolumePercent + change);
        SetStatus(F("Volumul podcastului: {0}%.", player.VolumePercent));
    }

    private void PodcastChooseSpeed()
    {
        if (!HasPodcast) { SetStatus(T("Articolul nu are un podcast.")); return; }
        var player = EnsurePodcastPlayer();
        var dialog = new PodcastRateDialog(T("Viteza de redare"), PodcastPlayer.MinimumSpeed, PodcastPlayer.MaximumSpeed, 0.05, player.Speed) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        player.SetSpeed(dialog.Value);
        SetStatus(F("Viteza de redare: {0}.", player.Speed.ToString("F2", System.Globalization.CultureInfo.CurrentCulture)));
    }

    private void PodcastChoosePitch()
    {
        if (!HasPodcast) { SetStatus(T("Articolul nu are un podcast.")); return; }
        var player = EnsurePodcastPlayer();
        var dialog = new PodcastRateDialog(T("Tonul redării"), PodcastPlayer.MinimumPitch, PodcastPlayer.MaximumPitch, 0.05, player.Pitch) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        player.SetPitch(dialog.Value);
        SetStatus(F("Tonul redării: {0}.", player.Pitch.ToString("F2", System.Globalization.CultureInfo.CurrentCulture)));
    }

    private async Task PodcastDownloadAsync()
    {
        if (!HasPodcast) { SetStatus(T("Articolul nu are un podcast.")); return; }
        var url = _article.AudioUrl!;
        var fileName = Path.GetFileName(Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.AbsolutePath : "podcast.mp3");
        if (string.IsNullOrWhiteSpace(fileName)) fileName = "podcast.mp3";
        var dialog = new SaveFileDialog
        {
            Title = T("Salvează podcastul"),
            FileName = fileName,
            Filter = T("Fișiere audio") + "|*.mp3;*.m4a;*.aac;*.ogg;*.opus;*.wav|" + T("Toate fișierele") + "|*.*"
        };
        if (dialog.ShowDialog(this) != true) return;
        SetStatus(T("Se descarcă podcastul."));
        try
        {
            await using var source = await PodcastDownloadClient.GetStreamAsync(url);
            await using var target = File.Create(dialog.FileName);
            await source.CopyToAsync(target);
            SetStatus(F("Podcastul a fost salvat: {0}", dialog.FileName));
        }
        catch (Exception exception)
        {
            SetStatus(F("Descărcarea podcastului nu a reușit: {0}", exception.Message));
        }
    }

    private PodcastPlayer EnsurePodcastPlayer()
    {
        if (_podcast is not null) return _podcast;
        _podcast = new PodcastPlayer();
        return _podcast;
    }

    private void DisposePodcast()
    {
        _podcast?.Dispose();
        _podcast = null;
    }

    private static string PodcastClock(TimeSpan time) =>
        time.TotalHours >= 1 ? time.ToString(@"h\:mm\:ss") : time.ToString(@"mm\:ss");
}
