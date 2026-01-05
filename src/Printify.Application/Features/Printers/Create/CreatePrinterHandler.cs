using MediatR;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Create;

public sealed class CreatePrinterHandler(
    IPrinterRepository printerRepository,
    IPrinterListenerOrchestrator listenerOrchestrator)
    : IRequestHandler<CreatePrinterCommand, Printer>
{
    public async Task<Printer> Handle(
        CreatePrinterCommand request,
        CancellationToken ct)
    {
        var existing = await printerRepository
            .GetByIdAsync(request.PrinterId, request.Context.WorkspaceId, ct)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        if (request.Context.WorkspaceId is null)
            throw new AuthenticationFailedException("Workspace is not set.");

        var listenTcpPortNumber = await printerRepository.GetFreeTcpPortNumber(ct).ConfigureAwait(false);

        var printer = new Printer(
            request.PrinterId,
            request.Context.WorkspaceId.Value,
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
            null,
            null,
            false,
            false,
            null,
            null);

        await printerRepository.AddAsync(printer, ct).ConfigureAwait(false);

        // Seed realtime status so target state is stored outside the printer aggregate.
        var initialUpdate = new PrinterRealtimeStatusUpdate(
            printer.Id,
            DateTimeOffset.UtcNow,
            TargetState: PrinterTargetState.Started,
            State: PrinterState.Stopped);
        await printerRepository.UpsertRealtimeStatusAsync(initialUpdate, ct).ConfigureAwait(false);

        await listenerOrchestrator.AddListenerAsync(printer, PrinterTargetState.Started, ct).ConfigureAwait(false);

        return printer;
    }
}
