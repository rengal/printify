using System;
using System.Collections.Generic;
using System.Linq;
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
            printer.Protocol,
            printer.WidthInDots,
            printer.HeightInDots,
            printer.ListenTcpPortNumber,
            printer.IsPinned);
    }

    internal static IReadOnlyList<PrinterDto> ToDtos(this IEnumerable<Printer> printers)
    {
        ArgumentNullException.ThrowIfNull(printers);
        return printers.Select(ToDto).ToList();
    }
}
