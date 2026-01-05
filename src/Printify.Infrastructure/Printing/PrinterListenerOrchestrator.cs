using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Application.Interfaces;
using Printify.Domain.PrintJobs;
using Printify.Domain.Printers;

namespace Printify.Infrastructure.Printing;

public sealed class PrinterListenerOrchestrator(
    IPrinterListenerFactory listenerFactory,
    IPrintJobSessionsOrchestrator printJobSessions,
    IServiceScopeFactory scopeFactory,
    IPrinterStatusStream statusStream,
    ILogger<PrinterListenerOrchestrator> logger)
    : IPrinterListenerOrchestrator
{
    private readonly ConcurrentDictionary<Guid, IPrinterListener> listeners = new();
    private readonly ConcurrentDictionary<Guid, HashSet<IPrinterChannel>> printerChannels = new();

    public async Task AddListenerAsync(Printer printer, PrinterTargetState targetState, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(printer);

        await RemoveListenerAsync(printer, targetState, ct).ConfigureAwait(false);

        var listener = listenerFactory.Create(printer);
        logger.LogInformation("Listener added for printer {PrinterId}", printer.Id);
        listeners[printer.Id] = listener;
        listener.ChannelAccepted += Listener_ChannelAccepted;

        await UpdateRuntimeStatusAsync(printer, targetState, PrinterState.Starting, ct).ConfigureAwait(false);
        try
        {
            await listener.StartAsync(ct).ConfigureAwait(false);
            await UpdateRuntimeStatusAsync(printer, targetState, MapState(listener.Status), ct).ConfigureAwait(false);
        }
        catch
        {
            listener.ChannelAccepted -= Listener_ChannelAccepted;
            listeners.TryRemove(printer.Id, out _);
            await listener.DisposeAsync().ConfigureAwait(false);
            await UpdateRuntimeStatusAsync(printer, targetState, PrinterState.Error, ct).ConfigureAwait(false);
            throw;
        }
    }

    private async ValueTask Listener_ChannelAccepted(IPrinterListener listener, PrinterChannelAcceptedEventArgs args)
    {
        if (!listeners.ContainsKey(listener.PrinterId))
            return;

        var session = await printJobSessions.StartSessionAsync(args.Channel, CancellationToken.None).ConfigureAwait(false);
        var channel = args.Channel;
        session.DataTimedOut += Session_DataTimedOut;
        session.ResponseReady += async (s, e) =>
        {
            try
            {
                await channel.SendToClientAsync(e.Data, e.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send response to client for printer {PrinterId}", channel.Printer.Id);
            }
        };
        channel.DataReceived += Channel_DataReceived;
        channel.Closed += Channel_Closed;

        var channels = printerChannels.GetOrAdd(listener.PrinterId, static _ => new HashSet<IPrinterChannel>());
        channels.Add(channel);
    }

    private async ValueTask Session_DataTimedOut(IPrintJobSession session, PrintJobSessionDataTimedOutEventArgs args)
    {
        await printJobSessions.CompleteAsync(args.Channel, PrintJobCompletionReason.DataTimeout, CancellationToken.None);
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
            channels.Remove(channel);

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

    public async Task RemoveListenerAsync(Printer printer, PrinterTargetState targetState, CancellationToken ct)
    {
        if (!listeners.TryRemove(printer.Id, out var listener))
            return;

        if (printerChannels.TryRemove(printer.Id, out var channels))
        {
            foreach (var channel in channels)
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
        await UpdateRuntimeStatusAsync(printer, targetState, PrinterState.Stopped, ct).ConfigureAwait(false);
    }

    public ListenerStatusSnapshot GetStatus(Printer printer)
    {
        return !listeners.TryGetValue(printer.Id, out var listener)
            ? new ListenerStatusSnapshot(PrinterListenerStatus.Idle)
            : new ListenerStatusSnapshot(listener.Status);
    }

    public IReadOnlyCollection<IPrinterChannel> GetActiveChannels(Guid printerId)
    {
        return printerChannels.TryGetValue(printerId, out var channels)
            ? channels.ToArray()
            : [];
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

    private Task UpdateRuntimeStatusAsync(
        Printer printer,
        PrinterTargetState targetState,
        PrinterState status,
        CancellationToken ct)
    {
        var timestamp = DateTimeOffset.UtcNow;

        var update = new PrinterRealtimeStatusUpdate(
            printer.Id,
            timestamp,
            targetState,
            status);
        statusStream.Publish(printer.OwnerWorkspaceId, update);
        return Task.CompletedTask;
    }

    private static PrinterState MapState(PrinterListenerStatus status)
    {
        return status switch
        {
            PrinterListenerStatus.Idle => PrinterState.Stopped,
            PrinterListenerStatus.OpeningPort => PrinterState.Starting,
            PrinterListenerStatus.Listening => PrinterState.Started,
            PrinterListenerStatus.Failed => PrinterState.Error,
            _ => throw new InvalidOperationException("Unknown listener status")
        };
    }
}
