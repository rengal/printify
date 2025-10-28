using MediatR;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Update;

public sealed class UpdatePrinterHandler(
    IPrinterRepository printerRepository,
    IPrinterListenerOrchestrator listenerOrchestrator)
    : IRequestHandler<UpdatePrinterCommand, Printer>
{
    public async Task<Printer> Handle(UpdatePrinterCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var printer = await printerRepository.GetByIdAsync(
            request.PrinterId,
            request.Context.UserId,
            request.Context.AnonymousSessionId,
            cancellationToken).ConfigureAwait(false);

        if (printer is null)
        {
            throw new InvalidOperationException("Printer not found.");
        }

        var updated = printer with
        {
            DisplayName = request.DisplayName,
            Protocol = request.Protocol.ToString(),
            WidthInDots = request.WidthInDots,
            HeightInDots = request.HeightInDots,
            ListenTcpPortNumber = request.TcpListenPort ?? printer.ListenTcpPortNumber
        };

        await printerRepository.UpdateAsync(updated, cancellationToken).ConfigureAwait(false);

        if (printer.ListenTcpPortNumber != updated.ListenTcpPortNumber)
        {
            await listenerOrchestrator.RemoveListenerAsync(updated, cancellationToken).ConfigureAwait(false);
            await listenerOrchestrator.AddListenerAsync(updated, cancellationToken).ConfigureAwait(false);
        }

        return updated;
    }
}
