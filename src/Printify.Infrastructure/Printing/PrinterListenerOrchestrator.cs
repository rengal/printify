using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.PrintJobs;
using Printify.Domain.Printers;

namespace Printify.Infrastructure.Printing;

public sealed class PrinterListenerOrchestrator(
    IPrinterListenerFactory listenerFactory,
    IPrintJobSessionsOrchestrator printJobSessions,
    ILogger<PrinterListenerOrchestrator> logger)
    : IPrinterListenerOrchestrator
{
    private readonly ConcurrentDictionary<Guid, IPrinterListener> listeners = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<IPrinterChannel, byte>> printerChannels = new();

    public async Task AddListenerAsync(Printer printer, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(printer);

        await RemoveListenerAsync(printer, ct).ConfigureAwait(false);

        var listener = listenerFactory.Create(printer);
        logger.LogInformation("Listener added for printer {PrinterId}", printer.Id);
        listeners[printer.Id] = listener;
        listener.ChannelAccepted += Listener_ChannelAccepted;

        try
        {
            await listener.StartAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            listener.ChannelAccepted -= Listener_ChannelAccepted;
            listeners.TryRemove(printer.Id, out _);
            await listener.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async ValueTask Listener_ChannelAccepted(IPrinterListener listener, PrinterChannelAcceptedEventArgs args)
    {
        if (!listeners.ContainsKey(listener.PrinterId))
            return;

        await printJobSessions.StartSessionAsync(args.Channel, CancellationToken.None).ConfigureAwait(false);
        var channel = args.Channel;
        channel.DataReceived += Channel_DataReceived;
        channel.Closed += Channel_Closed;

        var channels = printerChannels.GetOrAdd(listener.PrinterId, static _ => new ConcurrentDictionary<IPrinterChannel, byte>());
        channels[channel] = 0;
    }

    private async ValueTask Channel_Closed(IPrinterChannel channel, PrinterChannelClosedEventArgs args)
    {
        if (listeners.ContainsKey(channel.Printer.Id))
        {
            await printJobSessions.CompleteAsync(channel, MapReason(args.Reason), CancellationToken.None)
                .ConfigureAwait(false);
        }

        channel.DataReceived -= Channel_DataReceived;
        channel.Closed -= Channel_Closed;

        var printerId = channel.Printer.Id;
        if (printerChannels.TryGetValue(printerId, out var channels))
        {
            channels.TryRemove(channel, out _);
        }

        await channel.DisposeAsync().ConfigureAwait(false);
    }

    private async ValueTask Channel_DataReceived(IPrinterChannel channel, PrinterChannelDataEventArgs args)
    {
        logger.LogDebug("Channel received {ByteCount} bytes for printer {PrinterId}", args.Buffer.Length, channel.Printer.Id);
        if (listeners.ContainsKey(channel.Printer.Id))
        {
            await printJobSessions.FeedAsync(channel, args.Buffer, CancellationToken.None).ConfigureAwait(false);
        }
    }

    public async Task RemoveListenerAsync(Printer printer, CancellationToken ct)
    {
        if (!listeners.TryRemove(printer.Id, out var listener))
            return;

        if (printerChannels.TryRemove(printer.Id, out var channels))
        {
            foreach (var channel in channels.Keys)
            {
                channel.DataReceived -= Channel_DataReceived;
                channel.Closed -= Channel_Closed;
                await printJobSessions.CompleteAsync(channel, PrintJobCompletionReason.Canceled, CancellationToken.None)
                    .ConfigureAwait(false);
                await channel.DisposeAsync().ConfigureAwait(false);
            }
        }

        await listener.StopAsync(ct).ConfigureAwait(false);
        await listener.DisposeAsync().ConfigureAwait(false);
        logger.LogInformation("Listener removed for printer {PrinterId}", printer.Id);
        listener.ChannelAccepted -= Listener_ChannelAccepted;
    }

    public ListenerStatusSnapshot? GetStatus(Printer printer)
    {
        if (!listeners.TryGetValue(printer.Id, out var listener))
        {
            return new ListenerStatusSnapshot(PrinterListenerStatus.Unknown);
        }

        return new ListenerStatusSnapshot(listener.Status);
    }

    public IReadOnlyCollection<IPrinterChannel> GetActiveChannels(Guid printerId)
    {
        if (printerChannels.TryGetValue(printerId, out var channels))
        {
            return channels.Keys.ToArray();
        }

        return Array.Empty<IPrinterChannel>();
    }

    private static PrintJobCompletionReason MapReason(ChannelClosedReason reason)
    {
        return reason switch
        {
            ChannelClosedReason.Completed => PrintJobCompletionReason.ClientDisconnected,
            ChannelClosedReason.Cancelled => PrintJobCompletionReason.Canceled,
            ChannelClosedReason.Faulted => PrintJobCompletionReason.Faulted,
            _ => PrintJobCompletionReason.ClientDisconnected
        };
    }
}
