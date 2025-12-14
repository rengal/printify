namespace Printify.Web.Contracts.Printers.Requests;

/// <summary>
/// Payload used to register a new printer.
/// </summary>
/// <param name="Id">Client-generated identifier.</param>
/// <param name="DisplayName">Friendly name shown in UI.</param>
/// <param name="Protocol">Protocol the printer expects (e.g., escpos).</param>
/// <param name="WidthInDots">Configured print width in dots.</param>
/// <param name="HeightInDots">Optional maximum height in dots when known.</param>
public sealed record CreatePrinterRequestDto(
    Guid Id,
    string DisplayName,
    string Protocol,
    int WidthInDots,
    int? HeightInDots,
    bool EmulateBufferCapacity,
    decimal? BufferDrainRate,
    int? BufferMaxCapacity);
