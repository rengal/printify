using System.Globalization;
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
        IEscPosCommandTrieProvider commandTrieProvider)
        : base(clockFactory, job, channel)
    {
        ArgumentNullException.ThrowIfNull(clockFactory);
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(commandTrieProvider);
        idleClock = clockFactory.Create();
        parser = new EscPosParser(commandTrieProvider, OnElement);
    }

    public override Task Feed(ReadOnlyMemory<byte> input, CancellationToken ct)
    {
        if (IsCompleted)
            return Task.CompletedTask;

        parser.Feed(input.Span, ct);

        idleClock.Restart();
        _ = IdleTimeoutAsync(ct);
        return base.Feed(input, ct);
    }

    private void OnElement(Element element)
    {
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
            // expected if new data arrives or job is canceled
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
        var document = new Document(
            Guid.NewGuid(),
            Job.Id,
            Printer.Id,
            DateTimeOffset.UtcNow,
            Printer.Protocol,
            Channel.ClientAddress,
            snapshot);
        SetDocument(document);

        return base.Complete(reason);
    }

    private void TriggerOverflow()
    {
        if (!HasOverflow)
            return;

        var message = string.Format(CultureInfo.InvariantCulture, "Simulated buffer overflow after {0:0} bytes",
            bufferedBytes);
        ElementBuffer.Add(new PrinterError(message));
    }

    private static bool TryResolveBarcodeSymbology(byte value, out BarcodeSymbology symbology, out bool usesLengthIndicator)
    {
        switch (value)
        {
            case 0x00:
            case 0x41:
                symbology = BarcodeSymbology.UpcA;
                usesLengthIndicator = value >= 0x41;
                return true;
            case 0x01:
            case 0x42:
                symbology = BarcodeSymbology.UpcE;
                usesLengthIndicator = value >= 0x42;
                return true;
            case 0x02:
            case 0x43:
                symbology = BarcodeSymbology.Ean13;
                usesLengthIndicator = value >= 0x43;
                return true;
            case 0x03:
            case 0x44:
                symbology = BarcodeSymbology.Ean8;
                usesLengthIndicator = value >= 0x44;
                return true;
            case 0x04:
            case 0x45:
                symbology = BarcodeSymbology.Code39;
                usesLengthIndicator = value >= 0x45;
                return true;
            case 0x05:
            case 0x46:
                symbology = BarcodeSymbology.Itf;
                usesLengthIndicator = value >= 0x46;
                return true;
            case 0x06:
            case 0x47:
                symbology = BarcodeSymbology.Codabar;
                usesLengthIndicator = value >= 0x47;
                return true;
            case 0x48:
                symbology = BarcodeSymbology.Code93;
                usesLengthIndicator = true;
                return true;
            case 0x49:
                symbology = BarcodeSymbology.Code128;
                usesLengthIndicator = true;
                return true;
            default:
                symbology = default;
                usesLengthIndicator = false;
                return false;
        }
    }
}
