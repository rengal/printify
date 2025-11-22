using MediatR;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Create;

public sealed class CreatePrinterHandler(
    IPrinterRepository printerRepository,
    IPrinterListenerOrchestrator listenerOrchestrator,
    IPrinterListenerFactory listenerFactory)
    : IRequestHandler<CreatePrinterCommand, Printer>
{
    public async Task<Printer> Handle(
        CreatePrinterCommand request,
        CancellationToken ct)
    {
        var existing = await printerRepository
            .GetByIdAsync(request.PrinterId, request.Context.UserId, request.Context.AnonymousSessionId, ct)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        var listenTcpPortNumber = request.TcpListenPort
            ?? await printerRepository.GetFreeTcpPortNumber(ct).ConfigureAwait(false);

        var printer = new Printer(
            request.PrinterId,
            request.Context.UserId,
            request.Context.AnonymousSessionId,
            request.DisplayName,
            request.Protocol,
            request.WidthInDots,
            request.HeightInDots,
            DateTimeOffset.UtcNow,
            request.Context.IpAddress,
            listenTcpPortNumber,
            request.EmulateBufferCapacity,
            request.BufferDrainRate,
            request.BufferMaxCapacity,
            false,
            false,
            null);

        await printerRepository.AddAsync(printer, ct).ConfigureAwait(false);

        await listenerOrchestrator.AddListenerAsync(printer, ct).ConfigureAwait(false);

        return printer;
    }
}
