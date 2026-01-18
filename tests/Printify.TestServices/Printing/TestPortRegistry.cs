using System.Collections.Concurrent;

namespace Printify.TestServices.Printing;

/// <summary>
/// Test-only port registry with per-host lifetime to coordinate between allocator and listeners.
/// </summary>
public sealed class TestPortRegistry : ITestPortRegistry
{
    private readonly ConcurrentDictionary<int, byte> claimedPorts = new();

    public void ClaimPort(int port)
    {
        if (!claimedPorts.TryAdd(port, 0))
        {
            throw new InvalidOperationException($"Port {port} is already claimed in test registry.");
        }
    }

    public void ReleasePort(int port)
    {
        claimedPorts.TryRemove(port, out _);
    }
}
