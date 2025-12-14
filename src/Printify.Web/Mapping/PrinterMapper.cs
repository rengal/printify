using Printify.Domain.Printers;
using Printify.Web.Contracts.Printers.Responses;

namespace Printify.Web.Mapping;

internal static class PrinterMapper
{
    internal static PrinterDesiredStatus ToDesiredStatus(this string desiredStatus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(desiredStatus);
        return desiredStatus.Trim().ToLowerInvariant() switch
        {
            "started" or "start" => PrinterDesiredStatus.Started,
            "stopped" or "stop" => PrinterDesiredStatus.Stopped,
            _ => throw new ArgumentOutOfRangeException(nameof(desiredStatus), desiredStatus, "Unsupported desired status.")
        };
    }

    internal static PrinterResponseDto ToResponseDto(this Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer);
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
            printer.DesiredStatus.ToString(),
            printer.RuntimeStatus.ToString(),
            printer.RuntimeStatusUpdatedAt,
            printer.RuntimeStatusError,
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
