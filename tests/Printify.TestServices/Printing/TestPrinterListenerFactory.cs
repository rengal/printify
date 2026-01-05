using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.TestServices.Printing;

public sealed class TestPrinterListenerFactory : IPrinterListenerFactory
{
    private static readonly ConcurrentDictionary<Guid, TestPrinterListener> listeners = new();
    private readonly IServiceProvider serviceProvider;

    public TestPrinterListenerFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public IPrinterListener Create(Printer printer, PrinterSettings settings)
    {
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentNullException.ThrowIfNull(settings);

        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var listener = new TestPrinterListener(printer, settings, scopeFactory);
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
