using System;
using Microsoft.Extensions.Logging;
using Printify.Application.Printing;
using Printify.Domain.Printers;
using Printify.Infrastructure.Printing.Tcp;

namespace Printify.Infrastructure.Printing;

public sealed class PrinterListenerFactory(ILoggerFactory loggerFactory) : IPrinterListenerFactory
{
    public IPrinterListener Create(Printer printer, PrinterSettings settings)
    {
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentNullException.ThrowIfNull(settings);

        var logger = loggerFactory.CreateLogger<TcpPrinterListener>();
        return new TcpPrinterListener(printer, settings, logger);
    }
}
