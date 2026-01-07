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
    public override event Func<IPrintJobSession, PrintJobSessionResponseEventArgs, ValueTask>? ResponseReady;

    protected override void OnResponseReady(PrintJobSessionResponseEventArgs args)
    {
        ResponseReady?.Invoke(this, args);
    }

    static EscPosPrintJobSession()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
  
    private IList<Element> ElementBuffer => MutableElements;
    private readonly IClock idleClock;
    private readonly EscPosParser parser;
    private readonly IPrinterBufferCoordinator bufferCoordinator;

    public EscPosPrintJobSession(
        IPrinterBufferCoordinator bufferCoordinator,
        IClockFactory clockFactory,
        PrintJob job,
        IPrinterChannel channel,
        IEscPosCommandTrieProvider trieProvider)
        : base(bufferCoordinator, job, channel)
    {
        ArgumentNullException.ThrowIfNull(bufferCoordinator);
        ArgumentNullException.ThrowIfNull(clockFactory);
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(trieProvider);
        this.bufferCoordinator = bufferCoordinator;
        idleClock = clockFactory.Create();
        parser = new EscPosParser(trieProvider, GetAvailableBytes, OnElement, OnResponse);
    }

    private void OnResponse(ReadOnlyMemory<byte> data)
    {
        SendResponse(data);
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
        if (element.LengthInBytes > 0)
        {
            bufferCoordinator.AddBytes(Printer, Settings, element.LengthInBytes);
        }

        ElementBuffer.Add(element);
    }

    private int GetAvailableBytes()
    {
        return bufferCoordinator.GetAvailableBytes(Printer, Settings);
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

        parser.Complete();


        var snapshot = ElementBuffer.ToArray();
        // Capture the printer dimensions so persisted documents reflect the exact rendering context.
        var document = new Document(
            Guid.NewGuid(),
            Job.Id,
            Printer.Id,
            Document.CurrentVersion,
            DateTimeOffset.UtcNow,
            Settings.Protocol,
            Settings.WidthInDots,
            Settings.HeightInDots,
            Channel.ClientAddress,
            snapshot);
        SetDocument(document);

        return base.Complete(reason);
    }
}
