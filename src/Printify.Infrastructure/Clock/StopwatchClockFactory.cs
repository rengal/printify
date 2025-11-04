using Printify.Domain.Core;
using Printify.Domain.Services;

namespace Printify.Infrastructure.Clock;

/// <summary>
/// Creates stopwatch-backed clock instances for latency simulations.
/// </summary>
public sealed class StopwatchClockFactory : IClockFactory
{
    public IClock Create()
    {
        return new StopwatchClock();
    }

    public void AdvanceAll(TimeSpan delta)
    {
        // Real stopwatch-backed clocks cannot be advanced manually in production.
        // Throw explicitly to avoid silent misuse in tests.
        throw new NotSupportedException("AdvanceAll is not supported for real stopwatch-backed clocks.");
    }
}
