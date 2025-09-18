using System.Diagnostics;
using Printify.Contracts.Service;

namespace Printify.Core.Service;

/// <summary>
/// Default clock implementation backed by <see cref="Stopwatch"/>.
/// </summary>
public sealed class StopwatchClock : IClock
{
    private readonly Stopwatch stopwatch = new Stopwatch();

    public void Start()
    {
        stopwatch.Reset();
        stopwatch.Start();
    }

    public long ElapsedMs => stopwatch.ElapsedMilliseconds;
}
