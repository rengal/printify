namespace Printify.Application.Printing;

public interface IPrinterStatusStream
{
    IAsyncEnumerable<PrinterStatusEvent> Subscribe(Guid workspaceId, CancellationToken ct);
    void Publish(PrinterStatusEvent statusEvent);
}
