using Printify.Contracts.Core;
using Printify.Contracts.Services;

namespace Printify.Core.Service;

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
