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
/// <param name="IsPinned">Indicates whether the printer is pinned for quick access.</param>
/// <param name="LastViewedDocumentId">Identifier of the last document viewed by the current user.</param>
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
    bool IsPinned,
    Guid? LastViewedDocumentId);
