using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Infrastructure.Printing;

public sealed class PrinterListenerFactory : IPrinterListenerFactory
{
    public IPrinterListener Create(Printer printer)
    {
        return new PrinterListener(printer);
    }
}
