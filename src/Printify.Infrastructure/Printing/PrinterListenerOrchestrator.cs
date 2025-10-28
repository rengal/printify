using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.PrintJobs;
using Printify.Domain.Printers;

namespace Printify.Infrastructure.Printing;

public sealed class PrinterListenerOrchestrator(
    IPrinterListenerFactory listenerFactory,
    IPrintJobsOrchestrator printJobsOrchestrator,
    ILogger<PrinterListenerOrchestrator> logger)
    : IPrinterListenerOrchestrator
{
    //private readonly ConcurrentDictionary<Guid, PrinterScope> printerScopes = new();
    private readonly ConcurrentDictionary<Guid, IPrinterListener> listeners = new();

    public async Task AddListenerAsync(Printer printer, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(printer);

        await RemoveListenerAsync(printer, ct).ConfigureAwait(false);

        var listener = listenerFactory.Create(printer);
        logger.LogInformation("Listener added for printer {PrinterId}", printer.Id);
        listeners[printer.Id] = listener;
        listener.ChannelAccepted += Listener_ChannelAccepted;

        await listener.StartAsync(ct).ConfigureAwait(false);
    }

    private async ValueTask Listener_ChannelAccepted(IPrinterListener listener, PrinterChannelAcceptedEventArgs args)
    {
        await printJobsOrchestrator.StartJobAsync(args.Channel, CancellationToken.None);
        var channel = args.Channel;
        channel.DataReceived += Channel_DataReceived;


        //todo move to printJob orchestrator
        // //var job =
        // if (!printerScopes.TryGetValue(listener.PrinterId, out var scope))
        // {
        //     await args.Channel.DisposeAsync().ConfigureAwait(false);
        //     return;
        // }
        //
        // channel.Closed += Channel_Closed;
        // scope.ActiveJobs[channel] = job;
        // TODO (?): Capture the channel subscriptions inside a small ChannelScope so removal logic can cleanly detach handlers.
    }

    private async ValueTask Channel_Closed(IPrinterChannel channel, PrinterChannelClosedEventArgs args)
    {
        await printJobsOrchestrator.StopJobAsync(channel, PrintJobCompletionReason.ClientDisconnected,
            CancellationToken.None);

        //todo move to printJobsOrchestrator implementation

        // if (!TryLocateChannel(channel, out var scope, out var printJob))
        //     return;
        //
        // logger.LogInformation("Channel closed for printer {PrinterId} with reason {Reason}", scope.Printer.Id, args.Reason);
        //
        // printJobsOrchestrator.StopJobAsync(printJob, PrintJobCompletionReason.ClientDisconnected, CancellationToken.None)
        // if (printJob != null)
        // {
        //     await printJob.State.Complete();
        // }
        //
        // channel.DataReceived -= Channel_DataReceived;
        // channel.Closed -= Channel_Closed;
        // scope.ActiveJobs.TryRemove(channel, out _);
        //
        // // TODO: Invoke StopJobAsync on the print job orchestrator using the looked-up job and propagate the close reason.
        // // TODO: Dispose the channel once orchestration is complete.
        // await channel.DisposeAsync().ConfigureAwait(false);
    }

    private async ValueTask Channel_DataReceived(IPrinterChannel channel, PrinterChannelDataEventArgs args)
    {
        logger.LogDebug("Channel received {ByteCount} bytes for printer {PrinterId}", args.Buffer.Length, channel.Printer.Id);
        await printJobsOrchestrator.FeedDataAsync(channel, args.Buffer, CancellationToken.None);

        // if (!TryLocateChannel(channel, out var scope, out var job))
        // {
        //     return ValueTask.CompletedTask;
        // }
    }

    public async Task RemoveListenerAsync(Printer printer, CancellationToken ct)
    {
        if (!listeners.TryRemove(printer.Id, out var listener))
            return;

        await listener.StopAsync(ct);
        
        logger.LogInformation("Listener removed for printer {PrinterId}", printer.Id);
        listener.ChannelAccepted -= Listener_ChannelAccepted;

        //todo move to PrintJobOrchestrator
        // foreach (var channel in scope.ActiveJobs.Keys)
        // {
        //     channel.DataReceived -= Channel_DataReceived;
        //     channel.Closed -= Channel_Closed;
        //     scope.ActiveJobs.TryRemove(channel, out _);
        //     await channel.DisposeAsync().ConfigureAwait(false);
        // }
        //
        // await scope.Listener.StopAsync(ct).ConfigureAwait(false);
        // await scope.Listener.DisposeAsync().ConfigureAwait(false);
    }

    public ListenerStatusSnapshot? GetStatus(Printer printer)
    {
        return null; //todo
        // if (!printerScopes.TryGetValue(printer.Id, out var scope))
        // {
        //     return new ListenerStatusSnapshot(PrinterListenerStatus.Unknown);
        // }
        //
        // return new ListenerStatusSnapshot(scope.Listener.Status);
    }

    // private bool TryLocateChannel(IPrinterChannel channel, out PrinterScope scope, out PrintJob? job)
    // {
    //     foreach (var candidate in printerScopes.Values)
    //     {
    //         if (candidate.ActiveJobs.TryGetValue(channel, out job))
    //         {
    //             scope = candidate;
    //             return true;
    //         }
    //     }
    //
    //     scope = null!;
    //     job = null;
    //     return false;
    // }
    //
    // private sealed class PrinterScope
    // {
    //     internal PrinterScope(Printer printer, IPrinterListener listener)
    //     {
    //         Printer = printer;
    //         Listener = listener;
    //     }
    //
    //     public Printer Printer { get; }
    //     public IPrinterListener Listener { get; }
    //     public ConcurrentDictionary<IPrinterChannel, PrintJob?> ActiveJobs { get; } = new();
    // }
}
