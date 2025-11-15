using MediatR;
using Printify.Application.Interfaces;
using Printify.Domain.Documents;

namespace Printify.Application.Features.Printers.Documents.Get;

public sealed class GetPrinterDocumentHandler(
    IPrinterRepository printerRepository,
    IDocumentRepository documentRepository)
    : IRequestHandler<GetPrinterDocumentQuery, Document?>
{
    public async Task<Document?> Handle(GetPrinterDocumentQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var printer = await printerRepository.GetByIdAsync(
            request.PrinterId,
            request.Context.UserId,
            request.Context.AnonymousSessionId,
            cancellationToken).ConfigureAwait(false);

        if (printer is null)
        {
            return null;
        }

        var document = await documentRepository.GetByIdAsync(request.DocumentId, cancellationToken).ConfigureAwait(false);
        return document is not null && document.PrinterId == request.PrinterId
            ? document
            : null;
    }
}
