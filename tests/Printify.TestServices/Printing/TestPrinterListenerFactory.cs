using System;
using System.Collections.Concurrent;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.TestServices.Printing;

public sealed class TestPrinterListenerFactory : IPrinterListenerFactory
{
    private static readonly ConcurrentDictionary<Guid, TestPrinterListener> listeners = new();

    public IPrinterListener Create(Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer);

        var listener = new TestPrinterListener(printer);
        listeners[printer.Id] = listener;
        return listener;
    }

    public static bool TryGetListener(Guid printerId, out TestPrinterListener listener)
        => listeners.TryGetValue(printerId, out listener!);

    public static void Unregister(Guid printerId)
    {
        listeners.TryRemove(printerId, out _);
    }
}
