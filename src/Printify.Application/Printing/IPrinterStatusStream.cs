using Printify.Domain.Printers;

namespace Printify.Application.Printing;

public interface IPrinterStatusStream
{
    IAsyncEnumerable<PrinterStatusUpdate> Subscribe(Guid workspaceId, CancellationToken ct);
    void Publish(Guid workspaceId, PrinterStatusUpdate update);
}
