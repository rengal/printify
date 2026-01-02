using MediatR;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Domain.Documents.View;

namespace Printify.Application.Features.Printers.Documents.View;

public sealed class ListPrinterViewDocumentsHandler(
    IPrinterRepository printerRepository,
    IDocumentRepository documentRepository,
    IViewDocumentConverter viewDocumentConverter)
    : IRequestHandler<ListPrinterViewDocumentsQuery, IReadOnlyList<ViewDocument>>
{
    public async Task<IReadOnlyList<ViewDocument>> Handle(
        ListPrinterViewDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var printer = await printerRepository.GetByIdAsync(
            request.PrinterId,
            request.Context.WorkspaceId,
            cancellationToken).ConfigureAwait(false);

        if (printer is null)
            throw new PrinterNotFoundException(request.PrinterId);

        var documents = await documentRepository.ListByPrinterIdAsync(
            request.PrinterId,
            request.BeforeId,
            request.Limit,
            cancellationToken).ConfigureAwait(false);

        var viewDocuments = documents
            .Select(viewDocumentConverter.ToViewDocument)
            .ToList();

        return viewDocuments.AsReadOnly();
    }
}
