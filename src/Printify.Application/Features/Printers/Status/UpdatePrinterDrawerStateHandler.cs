using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Exceptions;
using Printify.Application.Features.Printers;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Status;

public sealed class UpdatePrinterDrawerStateHandler(
    IPrinterRepository printerRepository,
    IPrinterRuntimeStatusStore runtimeStatusStore,
    IPrinterStatusStream statusStream)
    : IRequestHandler<UpdatePrinterDrawerStateCommand, PrinterRuntimeStatus>
{
    public async Task<PrinterRuntimeStatus> Handle(
        IReceiveContext<UpdatePrinterDrawerStateCommand> context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        ArgumentNullException.ThrowIfNull(request);

        if (request.Drawer1State is null && request.Drawer2State is null)
        {
            throw new BadRequestException("At least one drawer state must be provided.");
        }

        var printer = await PrinterAccess
            .GetWritablePrinterAsync(printerRepository, request.Context, request.PrinterId, cancellationToken)
            .ConfigureAwait(false);

        var drawer1State = ParseDrawerState(request.Drawer1State);
        var drawer2State = ParseDrawerState(request.Drawer2State);

        var updatedAt = DateTimeOffset.UtcNow;
        var runtimeUpdate = new PrinterRuntimeStatusUpdate(
            printer.Id,
            updatedAt,
            Drawer1State: drawer1State,
            Drawer2State: drawer2State);
        var updated = runtimeStatusStore.Update(runtimeUpdate);

        var update = new PrinterStatusUpdate(
            printer.Id,
            updatedAt,
            RuntimeUpdate: runtimeUpdate);
        statusStream.Publish(printer.OwnerWorkspaceId, update);

        return updated;
    }

    private static DrawerState? ParseDrawerState(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Enum.TryParse<DrawerState>(value, true, out var parsed))
        {
            throw new BadRequestException($"Unsupported drawer state '{value}'.");
        }

        if (parsed == DrawerState.OpenedByCommand)
        {
            throw new BadRequestException("Drawer state 'OpenedByCommand' cannot be set via API.");
        }

        return parsed;
    }
}

