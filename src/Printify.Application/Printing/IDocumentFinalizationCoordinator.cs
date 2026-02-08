using Printify.Domain.Documents;

namespace Printify.Application.Printing;

public interface IDocumentFinalizationCoordinator
{
    Task<Document> FinalizeAsync(Document document, CancellationToken ct);
}
