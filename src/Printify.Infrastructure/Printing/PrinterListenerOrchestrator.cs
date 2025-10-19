
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Printify.Application.Printing;

namespace Printify.Infrastructure.Printing;

public sealed class PrinterListenerOrchestrator : IPrinterListenerOrchestrator
{
    private readonly ConcurrentDictionary<Guid, ListenerEntry> listeners = new();
    private readonly ILogger<PrinterListenerOrchestrator> logger;

    public PrinterListenerOrchestrator(ILogger<PrinterListenerOrchestrator> logger)
    {
        this.logger = logger;
    }

    public async Task AddListenerAsync(Guid printerId, IPrinterListener listener, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(listener);

        await RemoveListenerAsync(printerId, cancellationToken).ConfigureAwait(false);

        var entry = new ListenerEntry(listener);
        listeners[printerId] = entry;

        try
        {
            await listener.StartAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Listener started for printer {PrinterId}", printerId);
        }
        catch
        {
            listeners.TryRemove(printerId, out _);
            await listener.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public async Task RemoveListenerAsync(Guid printerId, CancellationToken cancellationToken)
    {
        if (!listeners.TryRemove(printerId, out var entry))
        {
            return;
        }

        try
        {
            await entry.Listener.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to stop listener for printer {PrinterId}", printerId);
        }
        finally
        {
            await entry.Listener.DisposeAsync().ConfigureAwait(false);
            logger.LogInformation("Listener removed for printer {PrinterId}", printerId);
        }
    }

    public ListenerStatusSnapshot GetStatus(Guid printerId)
    {
        var isActive = listeners.ContainsKey(printerId);
        return new ListenerStatusSnapshot(printerId, isActive, DateTimeOffset.UtcNow);
    }

    private sealed record ListenerEntry(IPrinterListener Listener);
}
