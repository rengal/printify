using Microsoft.Extensions.Logging;
using Printify.Application.Printing;
using Printify.Domain.Printers;
using Printify.Domain.Workspaces;
using Printify.Infrastructure.Printing.Tcp;

namespace Printify.Infrastructure.Printing;

public sealed class PrinterListenerFactory(ILoggerFactory loggerFactory, ITcpConnectionLog connectionLog) : IPrinterListenerFactory
{
    public IPrinterListener Create(Printer printer, PrinterSettings settings, Func<Workspace> getWorkspace)
    {
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(getWorkspace);

        var logger = loggerFactory.CreateLogger<TcpPrinterListener>();
        return new TcpPrinterListener(printer, settings, getWorkspace, connectionLog, logger);
    }
}
