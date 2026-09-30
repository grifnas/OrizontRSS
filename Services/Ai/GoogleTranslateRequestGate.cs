using System.Threading;

namespace CititorRSS.Jaws;

internal static class GoogleTranslateRequestGate
{
    private static int _isHeld;

    internal static IDisposable? TryAcquire()
    {
        if (Interlocked.CompareExchange(ref _isHeld, 1, 0) != 0) return null;
        return new Lease();
    }

    private sealed class Lease : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                Volatile.Write(ref _isHeld, 0);
        }
    }
}
