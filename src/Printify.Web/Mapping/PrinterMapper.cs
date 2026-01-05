using Printify.Application.Printing;
using Printify.Domain.Mapping;
using Printify.Domain.Printers;
using Printify.Web.Contracts.Printers.Responses;

namespace Printify.Web.Mapping;

internal static class PrinterMapper
{
    internal static PrinterState MapListenerState(PrinterListenerStatus status)
    {
        return status switch
        {
            PrinterListenerStatus.OpeningPort => PrinterState.Starting,
            PrinterListenerStatus.Listening => PrinterState.Started,
            PrinterListenerStatus.Idle => PrinterState.Stopped,
            PrinterListenerStatus.Failed => PrinterState.Error,
            _ => throw new InvalidOperationException("unknown runtime status")
        };
    }

    internal static PrinterResponseDto ToResponseDto(
        this Printer printer,
        ListenerStatusSnapshot runtime,
        PrinterRealtimeStatus? realtimeStatus)
    {
        ArgumentNullException.ThrowIfNull(printer);
        var state = MapListenerState(runtime.Status);
        // Default to Started to preserve legacy target-state behavior when no snapshot exists.
        var targetState = realtimeStatus?.TargetState ?? PrinterTargetState.Started;

        var effectiveRealtimeStatus = realtimeStatus is null
            ? null
            : realtimeStatus with
            {
                State = state
            };

        return new PrinterResponseDto(
            printer.Id,
            printer.DisplayName,
            DomainMapper.ToString(printer.Protocol),
            printer.WidthInDots,
            printer.HeightInDots,
            printer.ListenTcpPortNumber,
            printer.EmulateBufferCapacity,
            printer.BufferDrainRate,
            printer.BufferMaxCapacity,
            targetState.ToString(),
            state.ToString(),
            runtime?.Status == null ? null : DateTimeOffset.UtcNow,
            state == PrinterState.Error ? runtime?.Status.ToString() : null,
            ToRealtimeStatusDto(effectiveRealtimeStatus),
            printer.IsPinned,
            printer.LastViewedDocumentId,
            printer.LastDocumentReceivedAt);
    }

    internal static PrinterRealtimeStatusDto? ToRealtimeStatusDto(PrinterRealtimeStatus? status)
    {
        if (status is null)
        {
            return null;
        }

        return new PrinterRealtimeStatusDto(
            status.PrinterId,
            status.TargetState.ToString(),
            status.State.ToString(),
            status.UpdatedAt,
            status.Error,
            status.BufferedBytes,
            status.IsCoverOpen,
            status.IsPaperOut,
            status.IsOffline,
            status.HasError,
            status.IsPaperNearEnd,
            status.Drawer1State?.ToString(),
            status.Drawer2State?.ToString());
    }
}
