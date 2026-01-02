using MediatR;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Domain.Documents.View;

namespace Printify.Application.Features.Printers.Documents.View;

public sealed class GetPrinterViewDocumentHandler(
    IPrinterRepository printerRepository,
    IDocumentRepository documentRepository,
    IViewDocumentConverter viewDocumentConverter)
    : IRequestHandler<GetPrinterViewDocumentQuery, ViewDocument?>
{
    public async Task<ViewDocument?> Handle(GetPrinterViewDocumentQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var printer = await printerRepository.GetByIdAsync(
            request.PrinterId,
            request.Context.WorkspaceId,
            cancellationToken).ConfigureAwait(false);

        if (printer is null)
            throw new PrinterNotFoundException(request.PrinterId);

        var document = await documentRepository.GetByIdAsync(request.DocumentId, cancellationToken)
            .ConfigureAwait(false);

        if (document is null || document.PrinterId != request.PrinterId)
            return null;

        return viewDocumentConverter.ToViewDocument(document);
    }
}
