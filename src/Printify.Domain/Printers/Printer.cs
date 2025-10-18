namespace Printify.Domain.Printers;

/// <summary>
/// Represents virtual printer.
/// </summary>
/// <param name="Id">Database-generated identifier.</param>
/// <param name="OwnerUserId">Identifier of the user that owns the printer, if claimed.</param>
/// <param name="OwnerAnonymousSessionId">Identifier of the session that registered the printer.</param>
/// <param name="DisplayName">Friendly name shown in UI.</param>
/// <param name="Protocol">Protocol the printer expects (e.g., escpos).</param>
/// <param name="WidthInDots">Configured print width in dots.</param>
/// <param name="HeightInDots">Optional maximum height in dots when known.</param>
/// <param name="CreatedAt">Registration timestamp in UTC.</param>
/// <param name="CreatedFromIp">IP address captured when the printer was registered.</param>
/// <param name="ListenTcpPortNumber">Listener tcp port number.</param>
/// <param name="IsDeleted">Soft-delete marker for the printer.</param>
public sealed record Printer(
    Guid Id,
    Guid? OwnerUserId,
    Guid? OwnerAnonymousSessionId,
    string DisplayName,
    string Protocol,
    int WidthInDots,
    int? HeightInDots,
    DateTimeOffset CreatedAt,
    string CreatedFromIp,
    int ListenTcpPortNumber,
    bool IsDeleted)
    : BaseDomainEntity(Id, CreatedAt, IsDeleted);
