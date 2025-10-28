using MediatR;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Application.Printing;

namespace Printify.Application.Features.Printers.Delete;

public sealed class DeletePrinterHandler(
    IPrinterRepository printerRepository,
    IPrinterListenerOrchestrator listenerOrchestrator)
    : IRequestHandler<DeletePrinterCommand, Unit>
{
    public async Task<Unit> Handle(DeletePrinterCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var printer = await printerRepository
            .GetByIdAsync(request.PrinterId, request.Context.UserId, request.Context.AnonymousSessionId, ct)
            .ConfigureAwait(false);

        if (printer is null)
            throw new PrinterNotFoundException(request.PrinterId);

        await listenerOrchestrator.RemoveListenerAsync(printer, ct).ConfigureAwait(false);
        await printerRepository.DeleteAsync(printer, ct).ConfigureAwait(false);

        return Unit.Value;
    }
}
