using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Application.Mediation;
using Printify.Application.Printing;

namespace Printify.Application.Features.Printers.Documents.Inject;

public sealed class InjectPrinterDocumentHandler(
    IPrinterRepository printerRepository,
    IPrintJobSessionsOrchestrator printJobSessions)
    : IRequestHandler<InjectPrinterDocumentCommand, Unit>
{
    public async Task<Unit> Handle(IReceiveContext<InjectPrinterDocumentCommand> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        ArgumentNullException.ThrowIfNull(request);

        var printer = await printerRepository
            .GetByIdAsync(request.PrinterId, request.Context.WorkspaceId, cancellationToken)
            .ConfigureAwait(false);

        if (printer is null)
            throw new PrinterNotFoundException(request.PrinterId);

        var settings = await printerRepository
            .GetSettingsAsync(request.PrinterId, cancellationToken)
            .ConfigureAwait(false);

        if (settings is null)
            throw new PrinterNotFoundException(request.PrinterId);

        await printJobSessions
            .InjectDocumentAsync(printer, settings, request.Data, cancellationToken)
            .ConfigureAwait(false);

        return Unit.Value;
    }
}
