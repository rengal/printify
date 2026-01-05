namespace Printify.Web.Contracts.Printers.Responses;

/// <summary>
/// Printer metadata enriched with document statistics exposed to clients.
/// </summary>
/// <param name="Printer">Printer identity and metadata.</param>
/// <param name="Settings">Configuration required to operate the printer listener.</param>
/// <param name="OperationalFlags">Persisted operational flags and target state.</param>
/// <param name="RuntimeStatus">Runtime-only listener status snapshot.</param>
public sealed record PrinterResponseDto(
    PrinterDto Printer,
    PrinterSettingsDto Settings,
    PrinterOperationalFlagsDto? OperationalFlags,
    PrinterRuntimeStatusDto? RuntimeStatus);
