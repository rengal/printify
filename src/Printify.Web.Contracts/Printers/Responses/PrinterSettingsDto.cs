namespace Printify.Web.Contracts.Printers.Responses;

/// <summary>
/// Configuration required to start and operate a printer listener.
/// </summary>
/// <param name="Protocol">Protocol the printer expects (e.g., escpos).</param>
/// <param name="WidthInDots">Configured print width in dots.</param>
/// <param name="HeightInDots">Optional maximum height in dots when known.</param>
/// <param name="TcpListenPort">TCP port the listener binds to.</param>
/// <param name="EmulateBufferCapacity">Indicates whether buffer simulation is enabled.</param>
/// <param name="BufferDrainRate">Drain rate for the simulated buffer.</param>
/// <param name="BufferMaxCapacity">Maximum capacity of the simulated buffer.</param>
public sealed record PrinterSettingsDto(
    string Protocol,
    int WidthInDots,
    int? HeightInDots,
    int TcpListenPort,
    bool EmulateBufferCapacity,
    decimal? BufferDrainRate,
    int? BufferMaxCapacity);
