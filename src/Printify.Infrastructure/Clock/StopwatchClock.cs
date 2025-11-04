using System.Diagnostics;
using Printify.Domain.Core;

namespace Printify.Infrastructure.Clock;

/// <summary>
/// Default clock implementation backed by <see cref="Stopwatch"/>.
/// </summary>
public sealed class StopwatchClock : IClock
{
    private readonly Stopwatch stopwatch = new();

    public void Start()
    {
        stopwatch.Reset();
        stopwatch.Start();
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

        await Task.Delay(delay, ct).ConfigureAwait(false);
    }
}
