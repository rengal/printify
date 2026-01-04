using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.Core;
using Printify.Domain.Documents;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Printers;
using Printify.Domain.PrintJobs;
using Printify.Domain.Services;

namespace Printify.Infrastructure.Printing;

public abstract class PrintJobSession : IPrintJobSession
{
    #region IPrintJobSession Properties

    private readonly List<Element> elements = new();
    private Document? document;

    public int TotalBytesReceived { get; private set; }
    public int TotalBytesSent { get; private set; }
    public int TotalBytesSentToClient { get; private set; }
    public DateTimeOffset LastReceivedBytes { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSentToClient { get; private set; } = DateTimeOffset.MinValue;

    public abstract event Func<IPrintJobSession, PrintJobSessionDataTimedOutEventArgs, ValueTask>? DataTimedOut;
    public abstract event Func<IPrintJobSession, PrintJobSessionResponseEventArgs, ValueTask>? ResponseReady;

    public bool IsBufferBusy
    {
        get
        {
            UpdateBufferState();

            return Printer is { EmulateBufferCapacity: true, BufferDrainRate: > 0 } && bufferedBytes >= busyThreshold;
        }
    }

    public bool HasOverflow
    {
        get
        {
            UpdateBufferState();
            return Printer is { EmulateBufferCapacity: true } && bufferedBytes >= overflowThreshold;
        }
    }

    public bool IsCompleted { get; protected set; }
    public IReadOnlyList<Element> Elements => elements;
    public Document? Document => document;

    #endregion IPrintJobSession Properties

    #region Protected Properties

    protected PrintJob Job { get; }
    protected IPrinterChannel Channel { get; }
    protected Printer Printer => Job.Printer;
    public virtual Task Feed(ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        if (IsCompleted || data.Length == 0)
            return Task.CompletedTask;

        TotalBytesReceived += data.Length;
        LastReceivedBytes = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    public virtual Task Complete(PrintJobCompletionReason reason)
    {
        if (IsCompleted)
            return Task.CompletedTask;

        IsCompleted = true;
        return Task.CompletedTask;
    }

    protected IList<Element> MutableElements => elements;

    protected void SetDocument(Document value)
    {
        document = value;
    }

    #endregion

    #region Private Members

    protected int bufferedBytes;
    private readonly IClock drainClock;
    private readonly int busyThreshold;
    private readonly int overflowThreshold;
    private const double BusyThresholdRatio = 0.5;

    #endregion

    #region Constructor

    protected PrintJobSession(IClockFactory clockFactory, PrintJob job, IPrinterChannel channel)
    {
        Job = job;
        Channel = channel;
        busyThreshold = (int)(Printer.BufferMaxCapacity.GetValueOrDefault() * BusyThresholdRatio);
        overflowThreshold = Printer.BufferMaxCapacity.GetValueOrDefault();
        drainClock = clockFactory.Create();
        drainClock.Restart();
        bufferedBytes = Printer.BufferMaxCapacity.GetValueOrDefault() * 2; //todo debugnow
    }

    #endregion

    #region Protected Methods

    protected void UpdateBufferState()
    {
        if (!Printer.EmulateBufferCapacity || Printer.BufferDrainRate == 0)
        {
            bufferedBytes = 0;
            return;
        }

        var elapsedMs = drainClock.ElapsedMs;
        drainClock.Restart();

        if (elapsedMs <= 0)
            return;

        if (!Printer.BufferDrainRate.HasValue)
        {
            bufferedBytes = 0;
            return;
        }

        if (Printer.BufferDrainRate.Value <= 0)
            return;

        var drainedBytes = Printer.BufferDrainRate.Value * (elapsedMs / 1000.0m);
        if (drainedBytes > 0)
            bufferedBytes = Math.Max(0, (int)(bufferedBytes - drainedBytes));
    }

    /// <summary>
    /// Sends response data back to the client (e.g., status bytes).
    /// Fire-and-forget; failures are handled by the event subscriber.
    /// </summary>
    protected void SendResponse(ReadOnlyMemory<byte> data)
    {
        TotalBytesSentToClient += data.Length;
        LastSentToClient = DateTimeOffset.UtcNow;

        OnResponseReady(new PrintJobSessionResponseEventArgs(data, CancellationToken.None));
    }

    /// <summary>
    /// Raises the ResponseReady event. Override in derived classes to invoke the event.
    /// </summary>
    protected virtual void OnResponseReady(PrintJobSessionResponseEventArgs args)
    {
        // Base implementation does nothing; derived classes override to raise their event
    }

    #endregion
}
