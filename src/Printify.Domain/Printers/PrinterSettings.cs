namespace Printify.Domain.Printers;

/// <summary>
/// Configuration required to start and operate a printer listener.
/// Add fields here when they affect protocol, network binding, or buffer behavior.
/// </summary>
public sealed record PrinterSettings(
    Protocol Protocol,
    int WidthInDots,
    int? HeightInDots,
    int ListenTcpPortNumber,
    bool EmulateBufferCapacity,
    decimal? BufferDrainRate,
    int? BufferMaxCapacity);
