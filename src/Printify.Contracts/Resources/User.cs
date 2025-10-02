namespace Printify.Contracts.Resources;

/// <summary>
/// Person interacting with the system. Acts as a logical owner for printers and documents.
/// </summary>
/// <param name="Id">Database-generated identifier.</param>
/// <param name="DisplayName">Friendly name surfaced to UI.</param>
/// <param name="CreatedAt">Creation timestamp in UTC.</param>
/// <param name="CreatedFromIp">IP address captured when the user was registered.</param>
public sealed record User(
    long Id,
    string DisplayName,
    DateTimeOffset CreatedAt,
    string CreatedFromIp);
