using Printify.Application.Printing;
using Printify.Domain.Documents;
using Printify.Domain.Printers;
using Printify.Domain.PrintJobs;

namespace Printify.Infrastructure.Printing;

public abstract class PrintJobSession(PrintJob job, IPrinterChannel channel) : IPrintJobSession
{
    protected PrintJob Job { get; } = job;
    protected IPrinterChannel Channel { get; } = channel;
    protected Printer Printer => job.Printer;

    /// <summary>
    /// Bytes received from client
    /// </summary>
    public int TotalBytesReceived { get; private set; } = 0;
    public int TotalBytesSent { get; private set; } = 0;
    public bool IsCompleted { get; protected set; } = false;
    /// <summary>
    /// Bytes sent to client
    /// </summary>
    public DateTimeOffset LastReceivedBytes { get; private set; } = DateTimeOffset.Now;
    public bool IsBufferBusy { get; protected set; } = false;
    public bool HasOverflow { get; protected set; } = false;
    public Document? Document => null;

    public virtual Task Feed(ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        if (IsCompleted || data.Length == 0)
            return Task.CompletedTask;

        TotalBytesReceived += data.Length;
        LastReceivedBytes = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    public Task Complete(PrintJobCompletionReason reason)
    {
        if (IsCompleted)
            return Task.CompletedTask;

        IsCompleted = true;
        return Task.CompletedTask;
    }

    protected Task<bool> TrySend(ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        if (IsCompleted)
            return Task.FromResult(false);

        TotalBytesSent += data.Length;
        try
        {
            channel.WriteAsync(data, ct);
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private void UpdateBufferState()
    {
        if (!printer.IsBufferTrackingEnabled)
        {
            bufferedBytes = 0;
            lastDrainSampleMs = clock.ElapsedMs;
            return;
        }

        var currentMs = clock.ElapsedMs;

        if (bufferedBytes <= 0)
        {
            lastDrainSampleMs = currentMs;
            return;
        }

        if (!bufferOptions.DrainRate.HasValue)
        {
            bufferedBytes = 0;
            lastDrainSampleMs = currentMs;
            return;
        }

        if (bufferOptions.DrainRate.Value <= 0)
        {
            lastDrainSampleMs = currentMs;
            return;
        }

        var elapsedMs = currentMs - lastDrainSampleMs;
        if (elapsedMs <= 0)
        {
            return;
        }

        var drained = bufferOptions.DrainRate.Value * (elapsedMs / 1000.0);
        if (drained > 0)
        {
            // Reduce the buffered byte count while keeping it non-negative.
            bufferedBytes = Math.Max(0d, bufferedBytes - drained);
        }

        lastDrainSampleMs = currentMs;
    }
}