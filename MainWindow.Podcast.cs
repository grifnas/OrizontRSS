using System.Windows;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

// Podcasturi din afișarea standard: redare și pauză din meniul articolului, fără să se deschidă cititorul.
// Viteza și tonul se schimbă din cititor, unde există comenzile complete.
public partial class MainWindow
{
    private PodcastPlayer? _podcastPlayer;
    private bool _podcastPlayerBusy;

    private void PodcastPlayFromList_Click(object sender, RoutedEventArgs e) => _ = TogglePodcastFromListAsync();

    private async Task TogglePodcastFromListAsync()
    {
        var article = _article;
        if (article is null || string.IsNullOrWhiteSpace(article.AudioUrl)) { Say(T("Articolul nu are un podcast.")); return; }
        if (_podcastPlayerBusy) return;
        _podcastPlayer ??= new PodcastPlayer();
        var player = _podcastPlayer;
        try
        {
            if (!player.IsOpen || !string.Equals(player.Url, article.AudioUrl, StringComparison.Ordinal))
            {
                _podcastPlayerBusy = true;
                Say(T("Se pornește podcastul."));
                var url = article.AudioUrl;
                await Task.Run(() => player.Play(url));
                Say(F("Redare podcast: {0} din {1}.", PodcastClock(player.Position), PodcastClock(player.Duration)));
                return;
            }
            player.TogglePause();
            Say(player.IsPlaying
                ? F("Redare podcast: {0} din {1}.", PodcastClock(player.Position), PodcastClock(player.Duration))
                : F("Podcast pus pe pauză: {0}.", PodcastClock(player.Position)));
        }
        catch (Exception exception)
        {
            Say(F("Podcastul nu a putut fi redat: {0}", exception.Message));
        }
        finally { _podcastPlayerBusy = false; }
    }

    private void DisposeMainPodcast()
    {
        _podcastPlayer?.Dispose();
        _podcastPlayer = null;
    }

    private static string PodcastClock(TimeSpan time) =>
        time.TotalHours >= 1 ? time.ToString(@"h\:mm\:ss") : time.ToString(@"mm\:ss");
}
