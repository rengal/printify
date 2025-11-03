using System;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.TestServices.Printing;

public sealed class TestPrinterListenerFactory : IPrinterListenerFactory
{
    public IPrinterListener Create(Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer);

        return new TestPrinterListener(printer);
    }
}
