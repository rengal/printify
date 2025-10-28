using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.Printers;

namespace Printify.Infrastructure.Printing;

public sealed class PrinterListenerOrchestrator(IPrinterListenerFactory listenerFactory, ILogger<PrinterListenerOrchestrator> logger)
    : IPrinterListenerOrchestrator
{
    private readonly ConcurrentDictionary<Guid, IPrinterListener> listeners = new();
    private readonly ConcurrentDictionary<Guid, IPrinterChannel> channels = new();

    public async Task AddListenerAsync(Printer printer, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(printer);

        await RemoveListenerAsync(printer, ct).ConfigureAwait(false);
        var listener = listenerFactory.Create(printer);
        listeners[printer.Id] = listener;
        
        logger.LogInformation($"Listener added for printer {printer.Id}");

        listener.ChannelAccepted += Listener_ChannelAccepted;
        await listener.StartAsync(ct).ConfigureAwait(false);
    }

    private ValueTask Listener_ChannelAccepted(IPrinterListener listener, PrinterChannelAcceptedEventArgs args)
    {
        var channel = args.Channel;
        channels[listener.PrinterId] = channel;
        channel.DataReceived += Channel_DataReceived;
        channel.Closed += Channel_Closed;
        return ValueTask.CompletedTask;
    }

    private ValueTask Channel_Closed(IPrinterChannel channel, PrinterChannelClosedEventArgs args)
    {
        logger.LogInformation($"channel closed: reason={args.Reason}");
        listeners.TryRemove(channel.Printer.Id, out _);

        //todo 

        return ValueTask.CompletedTask;
    }

    private ValueTask Channel_DataReceived(IPrinterChannel channel, PrinterChannelDataEventArgs args)
    {
        logger.LogInformation($"channel received: length={args.Buffer.Length}");

        //todo printJob

        return ValueTask.CompletedTask;
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
