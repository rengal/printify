using Printify.Domain.Printers;
using Printify.Web.Contracts.Printers.Responses;

namespace Printify.Web.Mapping;

internal static class PrinterMapper
{
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
