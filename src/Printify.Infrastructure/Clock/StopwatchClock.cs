using System.Diagnostics;
using Printify.Domain.Core;

namespace Printify.Infrastructure.Clock;

/// <summary>
/// Default clock implementation backed by <see cref="Stopwatch"/>.
/// </summary>
public sealed class StopwatchClock : IClock
{
    private readonly Stopwatch stopwatch = new();
    private readonly object gate = new();
    private CancellationTokenSource restartCts = new();

    public void Restart()
    {
        lock (gate)
        {
            var oldCts = restartCts;
            restartCts = new CancellationTokenSource();
            stopwatch.Reset();
            stopwatch.Start();
            oldCts.Cancel();
            oldCts.Dispose();
        }
    }

    public long ElapsedMs => stopwatch.ElapsedMilliseconds;

    public void Advance(TimeSpan delta)
    {
        // Real stopwatch-backed clocks cannot be advanced manually in production.
        // Throw explicitly to avoid silent test misconfiguration.
        throw new NotSupportedException("Advance is not supported for real stopwatch-backed clocks.");
    }

    public async Task DelayAsync(TimeSpan delay, CancellationToken ct = default)
    {
        if (delay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delay), "Delay cannot be negative.");
        }
        if (delay == TimeSpan.Zero)
            return;

        CancellationTokenSource linkedCts;
        lock (gate)
        {
            var restartToken = restartCts.Token;
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, restartToken);
        }

        using (linkedCts)
        {
            await Task.Delay(delay, linkedCts.Token).ConfigureAwait(false);
        }
    }
}
