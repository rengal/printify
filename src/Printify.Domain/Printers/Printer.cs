namespace Printify.Domain.Printers;

/// <summary>
/// Represents virtual printer.
/// </summary>
/// <param name="Id">Database-generated identifier.</param>
/// <param name="OwnerWorkspaceId">Identifier of the workspace that owns the printer, if claimed.</param>
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
/// <param name="TargetState">
/// Target lifecycle state set by the operator. Drives listener start/stop behavior.
/// </param>
/// <param name="RuntimeStatus">
/// Last known runtime status of the listener (transient, may lag reality).
/// </param>
/// <param name="RuntimeStatusUpdatedAt">
/// Timestamp when <paramref name="RuntimeStatus"/> was last updated.
/// </param>
/// <param name="RuntimeStatusError">
/// Optional diagnostic message associated with <paramref name="RuntimeStatus"/>.
/// </param>
/// <param name="IsPinned">Indicates whether the printer is pinned for quick access.</param>
/// <param name="IsDeleted">Soft-delete marker for the printer.</param>
/// <param name="LastViewedDocumentId">Identifier of the last document viewed for this printer.</param>
/// <param name="LastDocumentReceivedAt">Timestamp of the most recently persisted document for this printer.</param>
public sealed record Printer(
    Guid Id,
    Guid OwnerWorkspaceId,
    string DisplayName,
    Protocol Protocol,
    int WidthInDots,
    int? HeightInDots,
    DateTimeOffset CreatedAt,
    string CreatedFromIp,
    int ListenTcpPortNumber,
    bool EmulateBufferCapacity,
    decimal? BufferDrainRate,
    int? BufferMaxCapacity,
    PrinterTargetState TargetState,
    PrinterRuntimeStatus RuntimeStatus,
    DateTimeOffset? RuntimeStatusUpdatedAt,
    string? RuntimeStatusError,
    bool IsPinned,
    bool IsDeleted,
    Guid? LastViewedDocumentId,
    DateTimeOffset? LastDocumentReceivedAt)
    : BaseDomainEntity(Id, CreatedAt, IsDeleted);
