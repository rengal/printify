namespace Printify.Web.Contracts.Workspaces.Responses;

/// <summary>A single TCP connection attempt entry for the last 10 minutes.</summary>
/// <param name="ClientIp">IP address of the connecting client (port stripped).</param>
/// <param name="ConnectedAt">When the connection was attempted.</param>
/// <param name="Allowed">True if the connection was accepted; false if rejected by the IP whitelist.</param>
/// <param name="ConnectionType">"Tcp" for raw TCP printer connections, "Web" for HTTP/browser connections.</param>
public sealed record TcpConnectionEntryDto(
    string ClientIp,
    DateTimeOffset ConnectedAt,
    bool Allowed,
    string ConnectionType);
