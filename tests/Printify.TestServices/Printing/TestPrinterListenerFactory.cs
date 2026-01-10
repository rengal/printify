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

    public static async Task<TestPrinterListener> GetListenerAsync(Guid printerId, int timeoutInMs = 2000, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeoutInMs);

        while (!cts.IsCancellationRequested)
        {
            if (listeners.TryGetValue(printerId, out var listener))
            {
                return listener;
            }

            await Task.Delay(100, cts.Token);
        }

        throw new InvalidOperationException($"Listener for printer {printerId} was not registered within {timeoutInMs}ms.");
    }

    public static void Unregister(Guid printerId)
    {
        listeners.TryRemove(printerId, out _);
    }
}
