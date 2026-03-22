using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.Documents;
using Printify.Domain.Printing;
using Printify.Domain.Printers;
using Printify.Domain.PrintJobs;
using Printify.Domain.Services;
using Printify.Infrastructure.Printing.EscPos.Commands;

namespace Printify.Infrastructure.Printing;

/// <summary>
/// Coordinates live print job sessions per printer channel.
/// </summary>
public sealed class PrintJobSessionsOrchestrator(
    IPrintJobSessionFactory printJobSessionFactory,
    IServiceScopeFactory scopeFactory,
    IPrinterDocumentStream documentStream,
    IPrinterBufferCoordinator bufferCoordinator,
    IPrinterStatusStream statusStream,
    IPrinterRuntimeStatusStore runtimeStatusStore)
    : IPrintJobSessionsOrchestrator
{
    private readonly ConcurrentDictionary<IPrinterChannel, IPrintJobSession> jobSessions = new();

    public async Task<IPrintJobSession> StartSessionAsync(IPrinterChannel channel, CancellationToken ct, bool skipBufferCheck = false)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ct.ThrowIfCancellationRequested();

        var printer = channel.Printer;

        await using var scope = scopeFactory.CreateAsyncScope();
        var printerRepository = scope.ServiceProvider.GetRequiredService<IPrinterRepository>();
        var printerSettings = await printerRepository.GetSettingsAsync(printer.Id, ct)
            .ConfigureAwait(false) ?? channel.Settings;

        var printJob = new PrintJob(
            Guid.NewGuid(),
            printer,
            printerSettings,
            channel.Settings.Protocol,
            DateTimeOffset.Now,
            channel.ClientAddress);

        var printJobRepository = scope.ServiceProvider.GetRequiredService<IPrintJobRepository>();
        await printJobRepository.AddAsync(printJob, ct).ConfigureAwait(false);

        var jobSession = await printJobSessionFactory.Create(printJob, channel, ct, skipBufferCheck);
        jobSessions[channel] = jobSession;
        return jobSession;
    }

    public Task<IPrintJobSession?> GetSessionAsync(IPrinterChannel channel, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ct.ThrowIfCancellationRequested();

        return jobSessions.TryGetValue(channel, out var session)
            ? Task.FromResult<IPrintJobSession?>(session)
            : Task.FromResult<IPrintJobSession?>(null);
    }

    public async Task FeedAsync(IPrinterChannel channel, ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        if (!jobSessions.TryGetValue(channel, out var session) || data.Length == 0)
            return;

        ct.ThrowIfCancellationRequested();
        await session.Feed(data, ct);
    }

    public async Task CompleteAsync(IPrinterChannel channel, PrintJobCompletionReason reason, CancellationToken ct)
    {
        if (!jobSessions.TryRemove(channel, out var session))
            return;
        await session.Complete(reason).ConfigureAwait(false);
        // Emit a final buffer update so subscribers see the latest drained state.
        bufferCoordinator.ForcePublish(channel.Printer, channel.Settings);

        var document = session.Document;
        if (document is not null)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var documentFinalizationCoordinator =
                scope.ServiceProvider.GetRequiredService<IDocumentFinalizationCoordinator>();
            var finalizedDocument = await documentFinalizationCoordinator
                .FinalizeAsync(document, ct)
                .ConfigureAwait(false);
            var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
            var printerRepository = scope.ServiceProvider.GetRequiredService<IPrinterRepository>();
            await documentRepository.AddAsync(finalizedDocument, ct).ConfigureAwait(false);
            await printerRepository.SetLastDocumentReceivedAtAsync(
                    finalizedDocument.PrinterId,
                    finalizedDocument.Timestamp,
                    ct)
                .ConfigureAwait(false);
            // Update cash drawers status, if needed
            var drawerUpdate = await TryUpdateDrawerStateFromElementsAsync(
                channel.Printer,
                finalizedDocument.Commands,
                ct).ConfigureAwait(false);
            if (drawerUpdate is not null)
            {
                if (drawerUpdate.RuntimeUpdate is not null)
                {
                    runtimeStatusStore.Update(drawerUpdate.RuntimeUpdate);
                }
                statusStream.Publish(channel.Printer.OwnerWorkspaceId, drawerUpdate);
            }
            documentStream.Publish(new DocumentStreamEvent(finalizedDocument));
        }
    }

    public async Task InjectDocumentAsync(Printer printer, PrinterSettings settings, ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentNullException.ThrowIfNull(settings);

        await using var channel = new InjectedChannel(printer, settings);
        var session = await StartSessionAsync(channel, ct, skipBufferCheck: true).ConfigureAwait(false);
        await FeedAsync(channel, data, ct).ConfigureAwait(false);
        await CompleteAsync(channel, PrintJobCompletionReason.ClientDisconnected, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Emits drawer state updates derived from ESC/POS pulse elements.
    /// </summary>
    private static Task<PrinterStatusUpdate?> TryUpdateDrawerStateFromElementsAsync(
        Printer printer,
        IReadOnlyCollection<Command> elements,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var (drawer1, drawer2) = GetDrawerStateUpdates(elements);
        if (drawer1 is null && drawer2 is null)
        {
            return Task.FromResult<PrinterStatusUpdate?>(null);
        }

        var updatedAt = DateTimeOffset.UtcNow;
        var runtimeUpdate = new PrinterRuntimeStatusUpdate(
            printer.Id,
            updatedAt,
            Drawer1State: drawer1,
            Drawer2State: drawer2);
        var update = new PrinterStatusUpdate(
            printer.Id,
            updatedAt,
            RuntimeUpdate: runtimeUpdate);

        return Task.FromResult<PrinterStatusUpdate?>(update);
    }

    private static (DrawerState? Drawer1State, DrawerState? Drawer2State) GetDrawerStateUpdates(
        IReadOnlyCollection<Command> elements)
    {
        DrawerState? drawer1 = null;
        DrawerState? drawer2 = null;

        foreach (var element in elements)
        {
            if (element is not EscPosPulse pulse)
            {
                continue;
            }

            // ESC/POS pin 0/1 maps to drawer 1/2 for emulated cash drawer pulses.
            if (pulse.Pin == 0)
            {
                drawer1 = DrawerState.OpenedByCommand;
            }
            else if (pulse.Pin == 1)
            {
                drawer2 = DrawerState.OpenedByCommand;
            }
        }

        return (drawer1, drawer2);
    }

    private sealed class InjectedChannel(Printer printer, PrinterSettings settings) : IPrinterChannel
    {
#pragma warning disable CS0067
        public event Func<IPrinterChannel, PrinterChannelDataEventArgs, ValueTask>? DataReceived;
        public event Func<IPrinterChannel, PrinterChannelClosedEventArgs, ValueTask>? Closed;
#pragma warning restore CS0067
        public Printer Printer { get; } = printer;
        public PrinterSettings Settings { get; } = settings;
        public string ClientAddress => "inject://api";
        public ValueTask SendToClientAsync(ReadOnlyMemory<byte> data, CancellationToken ct) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
