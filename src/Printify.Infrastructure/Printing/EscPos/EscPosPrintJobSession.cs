using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.Core;
using Printify.Domain.Documents;
using Printify.Domain.Printers;
using Printify.Domain.PrintJobs;
using Printify.Domain.Services;
using Printify.Domain.Printing;
using Printify.Infrastructure.Printing.EscPos.Parsers;

namespace Printify.Infrastructure.Printing.EscPos;

public class EscPosPrintJobSession : PrintJobSession
{
    public override event Func<IPrintJobSession, PrintJobSessionDataTimedOutEventArgs, ValueTask>? DataTimedOut;
    public override event Func<IPrintJobSession, PrintJobSessionResponseEventArgs, ValueTask>? ResponseReady;

    protected override void OnResponseReady(PrintJobSessionResponseEventArgs args)
    {
        ResponseReady?.Invoke(this, args);
    }

    static EscPosPrintJobSession()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private IList<Command> ElementBuffer => MutableElements;
    private readonly IClock idleClock;
    private readonly EscPosParser parser;
    private readonly IPrinterBufferCoordinator bufferCoordinator;
    private CancellationTokenSource? idleCts;

    public EscPosPrintJobSession(
        IPrinterBufferCoordinator bufferCoordinator,
        IClockFactory clockFactory,
        PrintJob job,
        IPrinterChannel channel,
        EscPosCommandTrieProvider trieProvider,
        IServiceScopeFactory? scopeFactory)
        : base(bufferCoordinator, job, channel)
    {
        ArgumentNullException.ThrowIfNull(bufferCoordinator);
        ArgumentNullException.ThrowIfNull(clockFactory);
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(trieProvider);
        this.bufferCoordinator = bufferCoordinator;
        idleClock = clockFactory.Create();
        parser = new EscPosParser(
            trieProvider,
            scopeFactory,
            Printer,
            Job.PrinterSettings,
            OnElement,
            OnResponse);
    }

    private void OnResponse(ReadOnlyMemory<byte> data)
    {
        SendResponse(data);
    }

    public override Task Feed(ReadOnlyMemory<byte> input, CancellationToken ct)
    {
        if (IsCompleted)
            return Task.CompletedTask;

        // Cancel any pending idle timer before processing the chunk
        idleCts?.Cancel();
        idleCts?.Dispose();
        idleCts = null;

        base.Feed(input, ct);
        parser.Feed(input.Span, ct);

        // Start a fresh idle timer now that the chunk is fully processed
        var cts = new CancellationTokenSource();
        idleCts = cts;
        idleClock.Restart();
        _ = IdleTimeoutAsync(CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token).Token);
        return Task.CompletedTask;
    }

    private void OnElement(Command element)
    {
        if (element.LengthInBytes > 0)
        {
            bufferCoordinator.AddBytes(Printer, Job.PrinterSettings, element.LengthInBytes);
        }

        ElementBuffer.Add(element);
    }

    private async Task IdleTimeoutAsync(CancellationToken ct)
    {
        try
        {
            await idleClock.DelayAsync(TimeSpan.FromMilliseconds(PrinterConstants.ListenerIdleTimeoutMs), ct);
            if (!IsCompleted && DataTimedOut != null)
            {
                var args = new PrintJobSessionDataTimedOutEventArgs(Channel, ct);
                await DataTimedOut.Invoke(this, args).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            //  expected when a new chunk arrives and cancels the pending timer
        }
    }

    public override Task Complete(PrintJobCompletionReason reason)
    {
        if (IsCompleted)
            return Task.CompletedTask;

        idleCts?.Cancel();
        idleCts?.Dispose();
        idleCts = null;

        parser.Complete();

        var snapshot = ElementBuffer.ToArray();
        var document = new Document(
            Guid.NewGuid(),
            Job.Id,
            Printer.Id,
            DateTimeOffset.UtcNow,
            Job.Protocol,
            // Capture the printer dimensions at print time so later rendering stays consistent.
            Channel.ClientAddress,
            TotalBytesReceived,
            TotalBytesSentToClient,
            Job.PrinterSettings.WidthInDots,
            Job.PrinterSettings.HeightInDots,
            snapshot,
            null);
        SetDocument(document);

        return base.Complete(reason);
    }
}
