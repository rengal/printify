namespace Printify.TestServcies.Timing;

using System;
using Printify.Contracts.Service;

public sealed class TestClockFactory : IClockFactory
{
    public IClock Create()
    {
        return new TestClock();
    }

    private sealed class TestClock : IClock
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
}
