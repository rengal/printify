namespace Printify.Domain.Printers;

/// <summary>
/// Aggregated printer details for full reads.
/// </summary>
public sealed record PrinterDetailsSnapshot(
    Printer Printer,
    PrinterSettings Settings,
    PrinterOperationalFlags? OperationalFlags,
    PrinterRuntimeStatus? RuntimeStatus);
