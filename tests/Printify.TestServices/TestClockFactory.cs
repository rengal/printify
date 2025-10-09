using Printify.Domain.Core;
using Printify.Domain.Services;

namespace Printify.TestServices;

public sealed class TestClockFactory : IClockFactory
{
    private readonly List<TestClock> clocks = new();
    private readonly object gate = new();

    public IClock Create()
    {
        var clock = new TestClock();
        lock (gate)
        {
            clocks.Add(clock);
        }

        return clock;
    }

    public List<TestClock> Clocks
    {
        get
        {
            lock (gate)
            {
                return clocks.ToList();
            }
        }
    }
}

public sealed class TestClock : IClock
{
    private long elapsed;

    public void Start()
    {
        // Reset the elapsed time when the clock starts.
        elapsed = 0;
    }

    public long ElapsedMs => elapsed;

    public void Advance(TimeSpan delta)
    {
        if (delta < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delta));
        }

        // Add milliseconds (rounded down) to the elapsed counter.
        elapsed += (long)delta.TotalMilliseconds;
    }
}
