using Printify.Application.Printing;
using Printify.Domain.Printers;
using Printify.Web.Contracts.Printers.Responses;

namespace Printify.Web.Mapping;

internal static class PrinterMapper
{
    internal static PrinterTargetState ToTargetState(this string targetState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetState);
        return targetState.Trim().ToLowerInvariant() switch
        {
            "started" or "start" => PrinterTargetState.Started,
            "stopped" or "stop" => PrinterTargetState.Stopped,
            _ => throw new ArgumentOutOfRangeException(nameof(targetState), targetState, "Unsupported target state.")
        };
    }

    internal static PrinterResponseDto ToResponseDto(this Printer printer, ListenerStatusSnapshot? runtime = null)
    {
        ArgumentNullException.ThrowIfNull(printer);
        var runtimeStatus = runtime?.Status switch
        {
            PrinterListenerStatus.OpeningPort => PrinterRuntimeStatus.Starting,
            PrinterListenerStatus.Listening => PrinterRuntimeStatus.Started,
            PrinterListenerStatus.Idle => PrinterRuntimeStatus.Stopped,
            PrinterListenerStatus.Failed => PrinterRuntimeStatus.Error,
            _ => PrinterRuntimeStatus.Unknown
        };

        return new PrinterResponseDto(
            printer.Id,
            printer.DisplayName,
            printer.Protocol.ToDto(),
            printer.WidthInDots,
            printer.HeightInDots,
            printer.ListenTcpPortNumber,
            printer.EmulateBufferCapacity,
            printer.BufferDrainRate,
            printer.BufferMaxCapacity,
            printer.TargetState.ToString(),
            runtimeStatus.ToString(),
            runtime?.Status == null ? null : DateTimeOffset.UtcNow,
            runtimeStatus == PrinterRuntimeStatus.Error ? runtime?.Status.ToString() : null,
            printer.IsPinned,
            printer.LastViewedDocumentId,
            printer.LastDocumentReceivedAt);
    }

    internal static string ToDto(this Protocol protocol)
    {
        return protocol switch
        {
            Protocol.EscPos => "escpos",
            _ => throw new ArgumentOutOfRangeException(nameof(protocol), protocol, "Unsupported protocol value.")
        };
    }

}
