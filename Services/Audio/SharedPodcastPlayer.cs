namespace CititorRSS.Jaws;

/// <summary>
/// Un singur player pentru toată aplicația. Fereastra principală și cititorul comandă aceeași redare,
/// astfel încât pauza și volumul funcționează indiferent de unde a fost pornit podcastul.
/// </summary>
internal static class SharedPodcastPlayer
{
    private static readonly object Gate = new();
    private static PodcastPlayer? _player;

    public static PodcastPlayer Player
    {
        get
        {
            lock (Gate) return _player ??= new PodcastPlayer();
        }
    }

    public static void Dispose()
    {
        lock (Gate)
        {
            _player?.Dispose();
            _player = null;
        }
    }
}
