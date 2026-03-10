using Printify.Domain.Printers;
using Printify.Domain.Workspaces;

namespace Printify.Application.Printing;

public interface IPrinterListenerFactory
{
    IPrinterListener Create(Printer printer, PrinterSettings settings, Func<Workspace> getWorkspace);
}
