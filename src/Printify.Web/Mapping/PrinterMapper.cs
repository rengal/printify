using Printify.Domain.Printers;
using Printify.Web.Contracts.Printers.Responses;

namespace Printify.Web.Mapping;

internal static class PrinterMapper
{
    internal static PrinterDto ToDto(this Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer);
        return new PrinterDto(
            printer.Id,
            printer.DisplayName,
            printer.Protocol.ToDto(),
            printer.WidthInDots,
            printer.HeightInDots,
            printer.ListenTcpPortNumber,
            printer.EmulateBufferCapacity,
            printer.BufferDrainRate,
            printer.BufferMaxCapacity,
            printer.IsPinned);
    }

    internal static string ToDto(this Protocol protocol)
    {
        return protocol switch
        {
            Protocol.EscPos => "escpos",
            _ => throw new ArgumentOutOfRangeException(nameof(protocol), protocol, "Unsupported protocol value.")
        };
    }

    internal static IReadOnlyList<PrinterDto> ToDtos(this IEnumerable<Printer> printers)
    {
        ArgumentNullException.ThrowIfNull(printers);
        return printers.Select(ToDto).ToList();
    }
}
