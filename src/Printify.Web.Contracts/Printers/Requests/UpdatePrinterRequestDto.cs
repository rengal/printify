using System.Text.Json.Serialization;

namespace Printify.Web.Contracts.Printers.Requests;

/// <summary>
/// Payload used to update printer configuration.
/// </summary>
/// <param name="Printer">Printer identity payload.</param>
/// <param name="Settings">Printer configuration settings.</param>
public sealed partial record UpdatePrinterRequestDto
{
    [JsonConstructor]
    public UpdatePrinterRequestDto(PrinterDto printer, PrinterSettingsDto settings)
    {
        Printer = printer;
        Settings = settings;
    }

    public PrinterDto Printer { get; init; }

    public PrinterSettingsDto Settings { get; init; }
}

/// <summary>
/// Compatibility constructor for tests and tooling that still pass flat parameters.
/// </summary>
public sealed partial record UpdatePrinterRequestDto
{
    public UpdatePrinterRequestDto(
        string displayName,
        string protocol,
        int widthInDots,
        int? heightInDots,
        bool emulateBufferCapacity,
        decimal? bufferDrainRate,
        int? bufferMaxCapacity)
        : this(
            new PrinterDto(Guid.Empty, displayName),
            new PrinterSettingsDto(
                protocol,
                widthInDots,
                heightInDots,
                emulateBufferCapacity,
                bufferDrainRate,
                bufferMaxCapacity))
    {
    }
}
