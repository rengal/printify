namespace Printify.Domain.Requests;

/// <summary>
/// Contextual information captured for a single request, including the resolved session.
/// </summary>
/// <param name="WorkspaceId">Identifier of the workspace tied to the session.</param>
/// <param name="IpAddress">Client IP address observed for the request.</param>
public sealed record RequestContext(
    Guid? WorkspaceId,
    bool AuthValid,
    string IpAddress);
