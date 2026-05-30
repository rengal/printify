using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Exceptions;
using Printify.Application.Features.Printers;
using Printify.Application.Interfaces;
using Printify.Application.Mediation;
using Printify.Application.Printing;

namespace Printify.Application.Features.Printers.Documents.Import;

public sealed class ImportPrinterDocumentHandler(
    IPrinterRepository printerRepository,
    IPrintJobSessionsOrchestrator printJobSessions)
    : IRequestHandler<ImportPrinterDocumentCommand, Unit>
{
    public async Task<Unit> Handle(IReceiveContext<ImportPrinterDocumentCommand> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        ArgumentNullException.ThrowIfNull(request);

        var printer = await PrinterAccess
            .GetWritablePrinterAsync(printerRepository, request.Context, request.PrinterId, cancellationToken)
            .ConfigureAwait(false);

        var settings = await printerRepository
            .GetSettingsAsync(request.PrinterId, cancellationToken)
            .ConfigureAwait(false);

        if (settings is null)
            throw new PrinterNotFoundException(request.PrinterId);

        await printJobSessions
            .ImportDocumentAsync(printer, settings, request.Data, cancellationToken)
            .ConfigureAwait(false);

        return Unit.Value;
    }
}
