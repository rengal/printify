using System.Text;
using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.Core;
using Printify.Domain.Documents;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Printers;
using Printify.Domain.PrintJobs;
using Printify.Domain.Services;

namespace Printify.Infrastructure.Printing.EscPos;

public class EscPosPrintJobSession : PrintJobSession
{
    public override event Func<IPrintJobSession, PrintJobSessionDataTimedOutEventArgs, ValueTask>? DataTimedOut;

    static EscPosPrintJobSession()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
  
    private IList<Element> ElementBuffer => MutableElements;
    private readonly IClock idleClock;
    private readonly EscPosParser parser;

    public EscPosPrintJobSession(
        IClockFactory clockFactory,
        PrintJob job,
        IPrinterChannel channel,
        IEscPosCommandTrieProvider trieProvider)
        : base(clockFactory, job, channel)
    {
        ArgumentNullException.ThrowIfNull(clockFactory);
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(trieProvider);
        idleClock = clockFactory.Create();
        parser = new EscPosParser(trieProvider, GetAvailableBytes, OnElement);
    }

    public override Task Feed(ReadOnlyMemory<byte> input, CancellationToken ct)
    {
        if (IsCompleted)
            return Task.CompletedTask;

        base.Feed(input, ct);

        parser.Feed(input.Span, ct);

        idleClock.Restart();
        _ = IdleTimeoutAsync(ct);
        return  Task.CompletedTask;
    }

    private void OnElement(Element element)
    {
        // Drain before accounting for this element to reflect the printer buffer model.
        UpdateBufferState();
        if (Printer.EmulateBufferCapacity && Printer.BufferMaxCapacity is > 0 && element.LengthInBytes > 0)
            bufferedBytes += element.LengthInBytes;
        ElementBuffer.Add(element);
    }

    private int GetAvailableBytes()
    {
        if (!Printer.EmulateBufferCapacity || Printer.BufferMaxCapacity is null or <= 0)
            return int.MaxValue;

        // Use drained value so available capacity matches the model at request time.
        UpdateBufferState();
        return Math.Max(0, Printer.BufferMaxCapacity.Value - bufferedBytes);
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
        catch (OperationCanceledException e)
        {
            // expected if new data arrives or job is canceled
            Console.WriteLine(e.Message); //todo debugnow
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message); //todo debugnow
        }
    }

    public override Task Complete(PrintJobCompletionReason reason)
    {
        if (IsCompleted)
            return Task.CompletedTask;

        // Drain the buffer to capture the latest busy state before finalizing.
        UpdateBufferState();
        
        parser.Complete();


        var snapshot = ElementBuffer.ToArray();
        // Capture the printer dimensions so persisted documents reflect the exact rendering context.
        var document = new Document(
            Guid.NewGuid(),
            Job.Id,
            Printer.Id,
            Document.CurrentVersion,
            DateTimeOffset.UtcNow,
            Printer.Protocol,
            Printer.WidthInDots,
            Printer.HeightInDots,
            Channel.ClientAddress,
            snapshot);
        SetDocument(document);

        return base.Complete(reason);
    }
}
