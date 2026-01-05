namespace Printify.Web.Contracts.Printers.Responses;

/// <summary>
/// Sidebar snapshot containing printer metadata and state-only runtime status.
/// </summary>
/// <param name="Printer">Printer identity and metadata.</param>
/// <param name="RuntimeStatus">State-only runtime status.</param>
public sealed record PrinterSidebarSnapshotDto(
    PrinterDto Printer,
    PrinterRuntimeStatusDto? RuntimeStatus);
