namespace Printify.Application.Printing;

/// <summary>
/// Records recent raw TCP connection attempts per workspace for display in the IP whitelist UI.
/// </summary>
public interface ITcpConnectionLog
{
    /// <summary>Append a connection attempt to the log.</summary>
    void Record(Guid workspaceId, string clientIp, bool allowed, ConnectionType connectionType = ConnectionType.Tcp);

    /// <summary>Returns all connection attempts from the last 10 minutes for the workspace.</summary>
    IReadOnlyList<TcpConnectionEntry> GetRecent(Guid workspaceId);
}

public enum ConnectionType { Tcp, Web }

public sealed record TcpConnectionEntry(
    string ClientIp,
    DateTimeOffset ConnectedAt,
    bool Allowed,
    ConnectionType ConnectionType);
