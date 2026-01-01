using MediatR;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;

namespace Printify.Application.Features.Printers.Documents.Clear;

public sealed class ClearPrinterDocumentsHandler(
    IPrinterRepository printerRepository,
    IDocumentRepository documentRepository)
    : IRequestHandler<ClearPrinterDocumentsCommand, Unit>
{
    public async Task<Unit> Handle(ClearPrinterDocumentsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Ensure the printer belongs to the current workspace before deleting documents.
        var printer = await printerRepository.GetByIdAsync(
            request.PrinterId,
            request.Context.WorkspaceId,
            cancellationToken).ConfigureAwait(false);

        if (printer is null)
            throw new PrinterNotFoundException(request.PrinterId);

        // Delete all persisted documents tied to the printer in a single operation.
        await documentRepository.ClearByPrinterIdAsync(request.PrinterId, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
