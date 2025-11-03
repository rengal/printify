using Printify.Application.Printing;
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

    public int TotalBytesReceived { get; private set; }
    public int TotalBytesSent { get; private set; }
    public DateTimeOffset LastReceivedBytes { get; private set; } = DateTimeOffset.UtcNow;
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
            return Printer is { EmulateBufferCapacity: true, BufferDrainRate: > 0 } && bufferedBytes >= busyThreshold;
        }
    }

    public bool IsCompleted { get; protected set; }
    public IReadOnlyList<Element> Elements { get; protected set; } = new List<Element>();
    public Document? Document => null;

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
        idleClock.Start();
        return Task.CompletedTask;
    }

    public virtual Task Complete(PrintJobCompletionReason reason)
    {
        if (IsCompleted)
            return Task.CompletedTask;

        IsCompleted = true;
        return Task.CompletedTask;
    }

    #endregion

    #region Private Members

    protected int bufferedBytes;
    private readonly IClock idleClock;
    private readonly IClock drainClock;
    private readonly int busyThreshold;
    private const double BusyThresholdRatio = 0.5;

    #endregion

    #region Constructor

    protected PrintJobSession(IClockFactory clockFactory, PrintJob job, IPrinterChannel channel)
    {
        Job = job;
        Channel = channel;
        busyThreshold = (int)(Printer.BufferMaxCapacity.GetValueOrDefault() * BusyThresholdRatio);
        idleClock = clockFactory.Create();
        drainClock = clockFactory.Create();
    }

    #endregion

    #region Protected Methods

    protected async Task<bool> TrySend(ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        if (IsCompleted)
            return false;

        TotalBytesSent += data.Length;
        try
        {
            await Channel.WriteAsync(data, ct);
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    protected void UpdateBufferState()
    {
        if (!Printer.EmulateBufferCapacity || Printer.BufferDrainRate == 0)
        {
            bufferedBytes = 0;
            return;
        }

        var elapsedMs = drainClock.ElapsedMs;
        if (elapsedMs <= 0)
        {
            return;
        }

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
        drainClock.Start();
    }

    #endregion
}
