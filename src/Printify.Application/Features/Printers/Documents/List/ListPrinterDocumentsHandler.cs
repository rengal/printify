using MediatR;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Domain.Documents;

namespace Printify.Application.Features.Printers.Documents.List;

public sealed class ListPrinterDocumentsHandler(
    IPrinterRepository printerRepository,
    IDocumentRepository documentRepository)
    : IRequestHandler<ListPrinterDocumentsQuery, IReadOnlyList<Document>>
{
    public async Task<IReadOnlyList<Document>> Handle(ListPrinterDocumentsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var printer = await printerRepository.GetByIdAsync(
            request.PrinterId,
            request.Context.WorkspaceId,
            cancellationToken).ConfigureAwait(false);

        if (printer is null)
            throw new PrinterNotFoundException(request.PrinterId);

        return await documentRepository.ListByPrinterIdAsync(
            request.PrinterId,
            request.BeforeId,
            request.Limit,
            cancellationToken).ConfigureAwait(false);
    }
}
