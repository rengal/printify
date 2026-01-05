using Printify.Domain.Printers;

namespace Printify.Application.Printing;

public interface IPrinterStatusStream
{
    IAsyncEnumerable<PrinterRealtimeStatus> Subscribe(Guid workspaceId, CancellationToken ct);
    void Publish(Guid workspaceId, PrinterRealtimeStatus status);
}
