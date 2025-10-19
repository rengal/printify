using System;
using Printify.Application.Printing;

namespace Printify.Infrastructure.Printing;

public sealed class NullPrinterListenerFactory : IPrinterListenerFactory
{
    public IPrinterListener Create(Guid printerId, int port) => new NullPrinterListener();
}
