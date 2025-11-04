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
    private readonly List<(long TargetTime, TaskCompletionSource<bool> Tcs)> pendingDelayTasks = new();


    public void Start()
    {
        // Reset the elapsed time when the clock starts.
        elapsed = 0;
        pendingDelayTasks.Clear();
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

        // Complete all tasks whose target time has passed
        var readyTasks = pendingDelayTasks
            .Where(t => t.TargetTime <= elapsed)
            .ToList();

        foreach (var (target, tcs) in readyTasks)
        {
            tcs.TrySetResult(true);
            pendingDelayTasks.Remove((target, tcs));
        }
    }

    public Task DelayAsync(TimeSpan delay, CancellationToken ct = default)
    {
        if (delay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delay), "Delay cannot be negative.");
        }

        var target = elapsed + (long)delay.TotalMilliseconds;
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (ct.CanBeCanceled)
        {
            ct.Register(() => tcs.TrySetCanceled(ct));
        }
        pendingDelayTasks.Add((target, tcs));
        return tcs.Task;
    }
}
