namespace Printify.Application.Printing;

public interface IPrinterDocumentStream
{
    IAsyncEnumerable<DocumentStreamEvent> Subscribe(Guid printerId, CancellationToken ct);
    void Publish(DocumentStreamEvent documentEvent);
}
