using Printify.Domain.Printers;

namespace Printify.Application.Printing;

/// <summary>
/// Stores latest runtime status snapshots for active printers in memory.
/// </summary>
public interface IPrinterRuntimeStatusStore
{
    PrinterRuntimeStatus? Get(Guid printerId);
    PrinterRuntimeStatus Update(PrinterRuntimeStatusUpdate update);
}
