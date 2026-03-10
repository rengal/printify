using System.Collections.Concurrent;
using Printify.Application.Printing;

namespace Printify.Infrastructure.Printing;

/// <summary>
/// In-memory sliding-window log of raw TCP connection attempts, keyed by workspace.
/// Retains entries for the last 10 minutes; older entries are pruned on each access.
/// </summary>
public sealed class TcpConnectionLog : ITcpConnectionLog
{
    private static readonly TimeSpan RetentionWindow = TimeSpan.FromHours(1);

    // Each workspace has a lock-protected list sorted by ascending ConnectedAt.
    private readonly ConcurrentDictionary<Guid, List<TcpConnectionEntry>> store = new();

    public void Record(Guid workspaceId, string clientIp, bool allowed, ConnectionType connectionType = ConnectionType.Tcp)
    {
        var entry = new TcpConnectionEntry(clientIp, DateTimeOffset.UtcNow, allowed, connectionType);
        var list = store.GetOrAdd(workspaceId, _ => new List<TcpConnectionEntry>());
        lock (list)
        {
            list.Add(entry);
        }
    }

    public IReadOnlyList<TcpConnectionEntry> GetRecent(Guid workspaceId)
    {
        if (!store.TryGetValue(workspaceId, out var list))
            return [];

        var cutoff = DateTimeOffset.UtcNow - RetentionWindow;

        lock (list)
        {
            // Remove stale entries.
            list.RemoveAll(e => e.ConnectedAt < cutoff);
            return list.ToArray();
        }
    }
}
