using MediatR;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Documents.Clear;

public sealed class ClearPrinterDocumentsHandler(
    IPrinterRepository printerRepository,
    IDocumentRepository documentRepository,
    IPrinterStatusStream statusStream)
    : IRequestHandler<ClearPrinterDocumentsCommand, Unit>
{
    public async Task<Unit> Handle(ClearPrinterDocumentsCommand request, CancellationToken cancellationToken)
    {
        // Validate input to avoid null reference errors in downstream logic.
        ArgumentNullException.ThrowIfNull(request);

        // Ensure the printer belongs to the current workspace before deleting documents.
        var printer = await printerRepository.GetByIdAsync(
            request.PrinterId,
            request.Context.WorkspaceId,
            cancellationToken).ConfigureAwait(false);

        // Stop when the printer is missing or outside the current workspace.
        if (printer is null)
            throw new PrinterNotFoundException(request.PrinterId);

        // Delete all persisted documents tied to the printer in a single operation.
        await documentRepository.ClearByPrinterIdAsync(request.PrinterId, cancellationToken).ConfigureAwait(false);
        // Reset last document metadata so UI does not show deleted documents as the most recent.
        await printerRepository.ClearLastDocumentReceivedAtAsync(request.PrinterId, cancellationToken).ConfigureAwait(false);
        // Reload printer to obtain updated metadata for SSE publishing.
        var refreshedPrinter = await printerRepository
            .GetByIdAsync(request.PrinterId, request.Context.WorkspaceId, cancellationToken)
            .ConfigureAwait(false);
        if (refreshedPrinter is not null)
        {
            //refreshedPrinter.DisplayName = "NEw NAME!!!!";
            // Publish printer metadata changes so sidebar/active views refresh last document info.
            // The SSE update carries printer fields only, avoiding a full status snapshot.
            statusStream.Publish(
                refreshedPrinter.OwnerWorkspaceId,
                new PrinterStatusUpdate(
                    refreshedPrinter.Id,
                    DateTimeOffset.UtcNow,
                    Printer: refreshedPrinter));
        }

        return Unit.Value;
    }
}
