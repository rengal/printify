using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Infrastructure.Printing;

public sealed class PrinterListenerOrchestrator(IPrinterListenerFactory listenerFactory, ILogger<PrinterListenerOrchestrator> logger)
    : IPrinterListenerOrchestrator
{
    private readonly ConcurrentDictionary<Guid, IPrinterListener> listeners = new();

    public async Task AddListenerAsync(Printer printer, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(printer);

        await RemoveListenerAsync(printer, ct).ConfigureAwait(false);
        var listener = listenerFactory.Create(printer);
        listeners[printer.Id] = listener;
        
        logger.LogInformation($"Listener added for printer {printer.Id}");

        await listener.StartAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveListenerAsync(Printer printer, CancellationToken ct)
    {
        if (!listeners.TryRemove(printer.Id, out var listener))
            return;

        await listener.StopAsync(ct).ConfigureAwait(false);
        await listener.DisposeAsync().ConfigureAwait(false);
        logger.LogInformation($"Listener removed for printer {printer.Id}");
    }

    public ListenerStatusSnapshot? GetStatus(Printer printer)
    {
        if (!listeners.TryGetValue(printer.Id, out var listener))
            return new ListenerStatusSnapshot(PrinterListenerStatus.Unknown);
        return new ListenerStatusSnapshot(listener.Status);
    }
}
