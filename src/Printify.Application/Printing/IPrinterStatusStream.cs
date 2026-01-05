using Printify.Domain.Printers;

namespace Printify.Application.Printing;

public interface IPrinterStatusStream
{
    IAsyncEnumerable<PrinterRealtimeStatusUpdate> Subscribe(Guid workspaceId, CancellationToken ct);
    void Publish(Guid workspaceId, PrinterRealtimeStatusUpdate update);
}
