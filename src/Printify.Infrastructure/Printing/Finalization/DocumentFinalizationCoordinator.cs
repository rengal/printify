using Printify.Application.Printing;
using Printify.Domain.Documents;
using Printify.Domain.Printers;

namespace Printify.Infrastructure.Printing.Finalization;

public sealed class DocumentFinalizationCoordinator : IDocumentFinalizationCoordinator
{
    private readonly IReadOnlyDictionary<Protocol, IProtocolDocumentFinalizer> finalizersByProtocol;

    public DocumentFinalizationCoordinator(IEnumerable<IProtocolDocumentFinalizer> finalizers)
    {
        ArgumentNullException.ThrowIfNull(finalizers);

        var map = new Dictionary<Protocol, IProtocolDocumentFinalizer>();
        foreach (var finalizer in finalizers)
        {
            if (map.ContainsKey(finalizer.Protocol))
            {
                throw new InvalidOperationException(
                    $"A document finalizer for protocol '{finalizer.Protocol}' is already registered.");
            }

            map[finalizer.Protocol] = finalizer;
        }

        finalizersByProtocol = map;
    }

    public Task<Document> FinalizeAsync(Document document, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);
        ct.ThrowIfCancellationRequested();

        if (!finalizersByProtocol.TryGetValue(document.Protocol, out var finalizer))
        {
            return Task.FromResult(document);
        }

        return finalizer.FinalizeAsync(document, ct);
    }
}
