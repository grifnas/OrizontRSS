using System.Runtime.ExceptionServices;

namespace CititorRSS.Jaws;

/// <summary>Rezultatul descărcării unui feed; eroarea este păstrată pentru a fi tratată ca înainte, pe firul interfeței.</summary>
public sealed record FeedDownload(Feed Feed, RssLoadResult? Result, ExceptionDispatchInfo? Error);

/// <summary>
/// Descarcă feedurile în paralel, cu limită generală și limită pe site, ca un server lent să nu
/// blocheze restul listei și serverele să nu primească prea multe cereri simultane.
/// </summary>
public static class FeedDownloadScheduler
{
    public const int MaximumParallel = 6;
    public const int MaximumPerHost = 2;

    public static List<Task<FeedDownload>> Start(
        IEnumerable<Feed> feeds,
        Func<Feed, CancellationToken, Task<RssLoadResult>> download,
        CancellationToken cancellationToken,
        int maximumParallel = MaximumParallel,
        int maximumPerHost = MaximumPerHost)
    {
        var global = new SemaphoreSlim(maximumParallel);
        var hostGates = new Dictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);

        SemaphoreSlim HostGate(Feed feed)
        {
            var host = Uri.TryCreate(feed.Url, UriKind.Absolute, out var address) ? address.Host : feed.Url ?? string.Empty;
            lock (hostGates)
            {
                if (!hostGates.TryGetValue(host, out var gate)) hostGates[host] = gate = new SemaphoreSlim(maximumPerHost);
                return gate;
            }
        }

        async Task<FeedDownload> Run(Feed feed)
        {
            var hostGate = HostGate(feed);
            try
            {
                // Întâi locul pentru site, apoi locul general: un site cu multe feeduri nu ocupă toate locurile.
                await hostGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    await global.WaitAsync(cancellationToken).ConfigureAwait(false);
                    try
                    {
                        return new FeedDownload(feed, await download(feed, cancellationToken).ConfigureAwait(false), null);
                    }
                    finally
                    {
                        global.Release();
                    }
                }
                finally
                {
                    hostGate.Release();
                }
            }
            catch (Exception exception)
            {
                return new FeedDownload(feed, null, ExceptionDispatchInfo.Capture(exception));
            }
        }

        return feeds.Select(Run).ToList();
    }
}
