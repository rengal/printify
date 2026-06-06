using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Exceptions;
using Printify.Application.Features.Printers;
using Printify.Application.Interfaces;
using Printify.Application.Mediation;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Documents.Clear;

public sealed class ClearPrinterDocumentsHandler(
    IPrinterRepository printerRepository,
    IPrinterDocumentCleaner documentCleaner,
    IPrinterStatusStream statusStream)
    : IRequestHandler<ClearPrinterDocumentsCommand, Unit>
{
    public async Task<Unit> Handle(IReceiveContext<ClearPrinterDocumentsCommand> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        // Validate input to avoid null reference errors in downstream logic.
        ArgumentNullException.ThrowIfNull(request);

        // Ensure the printer belongs to the current workspace before deleting documents.
        var printer = await PrinterAccess
            .GetWritablePrinterAsync(printerRepository, request.Context, request.PrinterId, cancellationToken)
            .ConfigureAwait(false);

        // Delete the printer's documents along with their now-orphaned media rows/files; this also
        // resets the printer's last-document metadata so the UI stops showing deleted documents.
        await documentCleaner.DeleteByPrinterAsync(request.PrinterId, cancellationToken).ConfigureAwait(false);
        // Reload printer to obtain updated metadata for SSE publishing.
        var refreshedPrinter = await printerRepository
            .GetByIdAsync(request.PrinterId, request.Context.WorkspaceId, cancellationToken)
            .ConfigureAwait(false);
        if (refreshedPrinter is not null)
        {
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

