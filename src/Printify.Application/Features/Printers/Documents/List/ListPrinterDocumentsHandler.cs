using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Domain.Documents;

namespace Printify.Application.Features.Printers.Documents.List;

public sealed class ListPrinterDocumentsHandler(
    IPrinterRepository printerRepository,
    IDocumentRepository documentRepository)
    : IRequestHandler<ListPrinterDocumentsQuery, PrinterDocumentListResponse>
{
    public async Task<PrinterDocumentListResponse> Handle(
        IReceiveContext<ListPrinterDocumentsQuery> context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
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

        return new PrinterDocumentListResponse(documents);
    }
}

