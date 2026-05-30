using System.Runtime.CompilerServices;
using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Exceptions;
using Printify.Application.Features.Printers;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Status;

public sealed class StreamPrinterRuntimeHandler(
    IPrinterRepository printerRepository,
    IWorkspaceRepository workspaceRepository,
    IPrinterRuntimeStatusStore runtimeStatusStore,
    IPrinterStatusStream statusStream)
    : IRequestHandler<StreamPrinterRuntimeQuery, PrinterRuntimeStreamResult>
{
    public async Task<PrinterRuntimeStreamResult> Handle(
        IReceiveContext<StreamPrinterRuntimeQuery> context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        ArgumentNullException.ThrowIfNull(request);

        var printer = await PrinterAccess
            .GetReadablePrinterAsync(printerRepository, workspaceRepository, request.Context, request.PrinterId, cancellationToken)
            .ConfigureAwait(false);
        if (printer is null)
        {
            throw new PrinterNotFoundException(request.PrinterId);
        }

        var workspaceId = printer.OwnerWorkspaceId;
        // Subscribe eagerly so the channel is registered before the response headers are flushed.
        // Use CancellationToken.None here — the controller applies client-disconnect cancellation
        // via WithCancellation() when it iterates the result.
        var subscription = statusStream.Subscribe(workspaceId, CancellationToken.None);
        var updates = ReadUpdatesAsync(request.PrinterId, subscription, CancellationToken.None);

        return new PrinterRuntimeStreamResult("status", updates);
    }

    private async IAsyncEnumerable<PrinterStatusUpdate> ReadUpdatesAsync(
        Guid printerId,
        IAsyncEnumerable<PrinterStatusUpdate> subscription,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // Track last sent runtime status for this printer
        PrinterRuntimeStatus? lastSentRuntime = null;

        await foreach (var update in subscription.WithCancellation(ct))
        {
            if (update.PrinterId != printerId)
            {
                continue;
            }

            // Build partial runtime update based on what changed since last send
            var runtimeUpdate = update.RuntimeUpdate is not null
                ? BuildPartialRuntimeUpdate(lastSentRuntime, update.RuntimeUpdate)
                : null;

            // Update last sent tracker if we have runtime data
            if (update.RuntimeUpdate is not null)
            {
                var currentStatus = runtimeStatusStore.Get(printerId);
                if (currentStatus is not null)
                {
                    lastSentRuntime = currentStatus;
                }
            }

            // Only yield if there's something to send
            if (runtimeUpdate is not null
                || update.OperationalFlagsUpdate is not null
                || update.Settings is not null
                || update.Printer is not null)
            {
                yield return new PrinterStatusUpdate(
                    update.PrinterId,
                    update.UpdatedAt,
                    runtimeUpdate,
                    update.OperationalFlagsUpdate,
                    update.Settings,
                    update.Printer);
            }
        }
    }

    private static PrinterRuntimeStatusUpdate? BuildPartialRuntimeUpdate(
        PrinterRuntimeStatus? lastSent,
        PrinterRuntimeStatusUpdate incoming)
    {
        // First update or no previous state - send all non-null fields from incoming
        if (lastSent is null)
        {
            return incoming;
        }

        // Build partial update with only fields that differ from last sent
        return incoming with
        {
            State = incoming.State.HasValue && incoming.State.Value != lastSent.State
                ? incoming.State.Value
                : null,
            BufferedBytes = incoming.BufferedBytes.HasValue && incoming.BufferedBytes.Value != lastSent.BufferedBytes
                ? incoming.BufferedBytes.Value
                : null,
            Drawer1State = incoming.Drawer1State.HasValue && incoming.Drawer1State.Value != lastSent.Drawer1State
                ? incoming.Drawer1State.Value
                : null,
            Drawer2State = incoming.Drawer2State.HasValue && incoming.Drawer2State.Value != lastSent.Drawer2State
                ? incoming.Drawer2State.Value
                : null
        };
    }
}
