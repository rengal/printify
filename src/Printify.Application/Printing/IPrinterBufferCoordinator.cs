using Printify.Domain.Printers;

namespace Printify.Application.Printing;

/// <summary>
/// Coordinates simulated printer buffer state and publishes runtime updates.
/// </summary>
public interface IPrinterBufferCoordinator
{
    PrinterBufferSnapshot GetSnapshot(Printer printer, PrinterSettings settings);
    int GetAvailableBytes(Printer printer, PrinterSettings settings);
    void AddBytes(Printer printer, PrinterSettings settings, int byteCount);
    void ForcePublish(Printer printer, PrinterSettings settings);
}
