using Printify.Domain.Core;
using Printify.Domain.Services;

namespace Printify.Services.Clock;

/// <summary>
/// Creates stopwatch-backed clock instances for latency simulations.
/// </summary>
public sealed class StopwatchClockFactory : IClockFactory
{
    public IClock Create()
    {
        return new StopwatchClock();
    }
}
