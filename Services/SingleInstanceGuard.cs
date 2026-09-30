using System.Threading;

namespace CititorRSS.Jaws.Services;

/// <summary>Holds a per-session mutex for the lifetime of the primary application process.</summary>
internal sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex _mutex;
    private bool _disposed;

    private SingleInstanceGuard(Mutex mutex) => _mutex = mutex;

    public static SingleInstanceGuard? TryAcquire(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var mutex = new Mutex(initiallyOwned: false, name);
        var acquired = false;
        try
        {
            try
            {
                acquired = mutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                // The previous process ended unexpectedly; this thread now owns the mutex.
                acquired = true;
            }

            if (acquired) return new SingleInstanceGuard(mutex);
            mutex.Dispose();
            return null;
        }
        catch
        {
            if (!acquired) mutex.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
