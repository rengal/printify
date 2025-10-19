using System;

namespace Printify.Application.Printing;

public interface IPrinterListenerFactory
{
    IPrinterListener Create(Guid printerId, int port);
}
