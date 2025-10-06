namespace Printify.Contracts.Printers;

/// <summary>
/// Physical or virtual printer registered by a user or anonymous session.
/// </summary>
/// <param name="Id">Database-generated identifier.</param>
/// <param name="OwnerUserId">Identifier of the user that owns the printer, if claimed.</param>
/// <param name="OwnerSessionId">Identifier of the session that registered the printer.</param>
/// <param name="DisplayName">Friendly name shown in UI.</param>
/// <param name="Protocol">Protocol the printer expects (e.g., escpos).</param>
/// <param name="WidthInDots">Configured print width in dots.</param>
/// <param name="HeightInDots">Optional maximum height in dots when known.</param>
/// <param name="CreatedAt">Registration timestamp in UTC.</param>
/// <param name="CreatedFromIp">IP address captured when the printer was registered.</param>
public sealed record Printer(
    long Id,
    long? OwnerUserId,
    long OwnerSessionId,
    string DisplayName,
    string Protocol,
    int WidthInDots,
    int? HeightInDots,
    DateTimeOffset CreatedAt,
    string CreatedFromIp);
