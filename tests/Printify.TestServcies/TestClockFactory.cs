namespace Printify.TestServcies.Timing;

using Printify.Contracts.Service;

public sealed class TestClockFactory : IClockFactory
{
    public IClock Create()
    {
        return new TestClock();
    }

    private sealed class TestClock : IClock
    {
        public void Start()
        {
            // No-op
        }

        public long ElapsedMs => 0;
    }
}
