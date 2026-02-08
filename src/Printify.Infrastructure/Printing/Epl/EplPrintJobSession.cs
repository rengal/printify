using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.Core;
using Printify.Domain.Documents;
using Printify.Domain.Printers;
using Printify.Domain.PrintJobs;
using Printify.Domain.Services;
using Printify.Domain.Printing;
using Printify.Infrastructure.Printing.Epl.Parsers;

namespace Printify.Infrastructure.Printing.Epl;

public class EplPrintJobSession : PrintJobSession
{
    public override event Func<IPrintJobSession, PrintJobSessionDataTimedOutEventArgs, ValueTask>? DataTimedOut;
    public override event Func<IPrintJobSession, PrintJobSessionResponseEventArgs, ValueTask>? ResponseReady;

    protected override void OnResponseReady(PrintJobSessionResponseEventArgs args)
    {
        ResponseReady?.Invoke(this, args);
    }

    static EplPrintJobSession()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private IList<Command> ElementBuffer => MutableElements;
    private readonly IClock idleClock;
    private readonly EplParser parser;
    private readonly IPrinterBufferCoordinator bufferCoordinator;

    public EplPrintJobSession(
        IPrinterBufferCoordinator bufferCoordinator,
        IClockFactory clockFactory,
        PrintJob job,
        IPrinterChannel channel,
        EplCommandTrieProvider trieProvider,
        IServiceScopeFactory scopeFactory)
        : base(bufferCoordinator, job, channel)
    {
        ArgumentNullException.ThrowIfNull(bufferCoordinator);
        ArgumentNullException.ThrowIfNull(clockFactory);
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(trieProvider);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        this.bufferCoordinator = bufferCoordinator;
        idleClock = clockFactory.Create();
        parser = new EplParser(OnElement);
    }

    public override Task Feed(ReadOnlyMemory<byte> input, CancellationToken ct)
    {
        if (IsCompleted)
            return Task.CompletedTask;

        base.Feed(input, ct);

        parser.Feed(input.Span, ct);

        idleClock.Restart();
        _ = IdleTimeoutAsync(ct);
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
            // expected if new data arrives or job is canceled
        }
        catch (ObjectDisposedException)
        {
            // expected if data job finises
        }
        catch (Exception e)
        {
            Console.WriteLine(e); // todo debugnow
            throw;
        }
    }

    public override Task Complete(PrintJobCompletionReason reason)
    {
        if (IsCompleted)
            return Task.CompletedTask;

        parser.Complete();

        var snapshot = ElementBuffer.ToArray();
        var document = new Document(
            Guid.NewGuid(),
            Job.Id,
            Printer.Id,
            DateTimeOffset.UtcNow,
            Job.PrinterSettings.Protocol,
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
