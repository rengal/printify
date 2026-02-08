using Printify.Domain.Documents;
using Printify.Domain.Printers;

namespace Printify.Application.Printing;

public interface IProtocolDocumentFinalizer
{
    Protocol Protocol { get; }
    Task<Document> FinalizeAsync(Document document, CancellationToken ct);
}
