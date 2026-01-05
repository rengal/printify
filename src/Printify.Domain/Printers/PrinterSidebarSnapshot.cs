namespace Printify.Domain.Printers;

/// <summary>
/// Aggregated printer metadata for sidebar reads.
/// Runtime state is excluded and should be queried separately from listener services.
/// </summary>
public sealed record PrinterSidebarSnapshot(
    Printer Printer,
    PrinterRuntimeStatus? RuntimeStatus = null);
