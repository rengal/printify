using Printify.Domain.Printers;

namespace Printify.Application.Printing;

public interface IPrinterListenerFactory
{
   IPrinterListener Create(Printer printer, PrinterSettings settings);
}
