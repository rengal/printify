using System.Runtime.CompilerServices;
using MediatR;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Application.Features.Printers.Status;

public sealed class StreamPrinterStatusHandler(
    IPrinterRepository printerRepository,
    IPrinterStatusStream statusStream)
    : IRequestHandler<StreamPrinterStatusQuery, PrinterStatusStreamResult>
{
    public async Task<PrinterStatusStreamResult> Handle(
        StreamPrinterStatusQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Scope == PrinterRealtimeScope.Full && request.PrinterId is null)
        {
            throw new ArgumentException("Full scope requires printerId.", nameof(request));
        }

        if (request.PrinterId.HasValue)
        {
            // Guard against leaking status updates for printers outside the current workspace.
            var printer = await printerRepository
                .GetByIdAsync(request.PrinterId.Value, request.Context.WorkspaceId, cancellationToken)
                .ConfigureAwait(false);
            if (printer is null)
            {
                throw new PrinterNotFoundException(request.PrinterId.Value);
            }
        }

        var includeStateOnly = request.Scope == PrinterRealtimeScope.State;
        var eventName = includeStateOnly ? "state" : "full";
        var updates = ReadUpdatesAsync(
            request.Context.WorkspaceId.Value,
            request.PrinterId,
            includeStateOnly,
            cancellationToken);

        return new PrinterStatusStreamResult(eventName, updates);
    }

    private async IAsyncEnumerable<PrinterRealtimeStatusUpdate> ReadUpdatesAsync(
        Guid workspaceId,
        Guid? printerId,
        bool includeStateOnly,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // Stream updates from the workspace-wide status feed and filter by scope/printer.
        await foreach (var statusEvent in statusStream.Subscribe(workspaceId, ct))
        {
            if (printerId.HasValue && statusEvent.PrinterId != printerId.Value)
            {
                continue;
            }

            if (!MatchesScope(statusEvent, includeStateOnly))
            {
                continue;
            }

            yield return statusEvent;
        }
    }

    private static bool MatchesScope(PrinterRealtimeStatusUpdate status, bool includeStateOnly)
    {
        var hasRealtimePayload = status.BufferedBytes.HasValue
                                 || status.IsCoverOpen.HasValue
                                 || status.IsPaperOut.HasValue
                                 || status.IsOffline.HasValue
                                 || status.HasError.HasValue
                                 || status.IsPaperNearEnd.HasValue
                                 || status.Drawer1State.HasValue
                                 || status.Drawer2State.HasValue;
        var hasStateChange = status.State.HasValue
                             || status.TargetState.HasValue;
        // State scope tracks lifecycle changes (state, target, error) and excludes realtime payload updates.
        // Full scope tracks any update, including state changes and realtime payload fields.
        return includeStateOnly ? hasStateChange && !hasRealtimePayload : hasStateChange || hasRealtimePayload;
    }
}
