namespace Printify.Web.Contracts.Printers.Responses;

/// <summary>
/// Printer metadata enriched with document statistics exposed to clients.
/// </summary>
/// <param name="Id">Database-generated identifier.</param>
/// <param name="DisplayName">Friendly name shown in UI.</param>
/// <param name="Protocol">Protocol the printer expects (e.g., escpos).</param>
/// <param name="WidthInDots">Configured print width in dots.</param>
/// <param name="HeightInDots">Optional maximum height in dots when known.</param>
/// <param name="TcpListenPort">TCP port the listener binds to.</param>
/// <param name="EmulateBufferCapacity">Indicates whether buffer simulation is enabled.</param>
/// <param name="BufferDrainRate">Drain rate for the simulated buffer.</param>
/// <param name="BufferMaxCapacity">Maximum capacity of the simulated buffer.</param>
/// <param name="DesiredStatus">Desired lifecycle state (Started/Stopped).</param>
/// <param name="RuntimeStatus">Last known runtime state reported by the listener.</param>
/// <param name="RuntimeStatusUpdatedAt">Timestamp when <paramref name="RuntimeStatus"/> was captured.</param>
/// <param name="RuntimeStatusError">Optional diagnostic message if <paramref name="RuntimeStatus"/> is Error.</param>
/// <param name="IsPinned">Indicates whether the printer is pinned for quick access.</param>
/// <param name="LastViewedDocumentId">Identifier of the last viewed document</param>
/// <param name="LastDocumentReceivedAt">Timestamp of the most recently persisted document for this printer.</param>
public sealed record PrinterResponseDto(
    Guid Id,
    string DisplayName,
    string Protocol,
    int WidthInDots,
    int? HeightInDots,
    int TcpListenPort,
    bool EmulateBufferCapacity,
    decimal? BufferDrainRate,
    int? BufferMaxCapacity,
    string DesiredStatus,
    string RuntimeStatus,
    DateTimeOffset? RuntimeStatusUpdatedAt,
    string? RuntimeStatusError,
    bool IsPinned,
    Guid? LastViewedDocumentId,
    DateTimeOffset? LastDocumentReceivedAt);
