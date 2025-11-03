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
/// <param name="EmulateBufferCapacity">
/// Indicates whether the printer should simulate a finite input buffer.
/// When enabled, print jobs may be throttled based on the configured buffer rate and capacity.
/// </param>
/// <param name="BufferDrainRate">
/// Simulated rate at which the printer consumes buffered data, in bytes per second.
/// Used only when <paramref name="EmulateBufferCapacity"/> is enabled.
/// </param>
/// <param name="BufferMaxCapacity">
/// Maximum size of the emulated input buffer, in bytes.
/// When the buffer is full, additional writes are delayed until space becomes available.
/// </param>
/// <param name="IsPinned">Indicates whether the printer is pinned for quick access.</param>
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
    bool EmulateBufferCapacity,
    decimal? BufferDrainRate,
    int? BufferMaxCapacity,
    bool IsPinned,
    bool IsDeleted)
    : BaseDomainEntity(Id, CreatedAt, IsDeleted);
