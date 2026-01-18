using Printify.Application.Printing;
using Printify.Application.Printing.Events;
using Printify.Domain.Documents;
using Printify.Domain.Printing;
using Printify.Domain.Printers;
using Printify.Domain.PrintJobs;

namespace Printify.Infrastructure.Printing;

public abstract class PrintJobSession : IPrintJobSession
{
    #region IPrintJobSession Properties

    private readonly List<Command> elements = new();
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
            var snapshot = bufferCoordinator.GetSnapshot(Printer, Settings);
            return snapshot.IsBusy;
        }
    }

    public bool HasOverflow
    {
        get
        {
            var snapshot = bufferCoordinator.GetSnapshot(Printer, Settings);
            return snapshot.IsFull;
        }
    }

    public bool IsCompleted { get; protected set; }
    public IReadOnlyList<Command> Elements => elements;
    public Document? Document => document;

    #endregion IPrintJobSession Properties

    #region Protected Properties

    protected PrintJob Job { get; }
    protected IPrinterChannel Channel { get; }
    protected Printer Printer => Job.Printer;
    protected PrinterSettings Settings => Job.Settings;
    private readonly IPrinterBufferCoordinator bufferCoordinator;
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

    protected IList<Command> MutableElements => elements;

    protected void SetDocument(Document value)
    {
        document = value;
    }

    #endregion

    #region Constructor

    protected PrintJobSession(IPrinterBufferCoordinator bufferCoordinator, PrintJob job, IPrinterChannel channel)
    {
        ArgumentNullException.ThrowIfNull(bufferCoordinator);
        Job = job;
        Channel = channel;
        this.bufferCoordinator = bufferCoordinator;
    }

    #endregion

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
}
