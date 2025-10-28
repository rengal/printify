using System;
using System.Collections.Concurrent;
using Printify.Application.Printing;
using Printify.Domain.Documents;
using Printify.Domain.PrintJobs;

namespace Printify.Infrastructure.Printing;

/// <summary>
/// In-memory implementation that coordinates the lifecycle of active print jobs per printer channel.
/// </summary>
public sealed class PrintJobsOrchestrator(TimeProvider clock) : IPrintJobsOrchestrator
{
    private readonly TimeProvider timeProvider = clock ?? TimeProvider.System;
    private readonly ConcurrentDictionary<IPrinterChannel, PrintJobContext> activeJobs = new();

    public Task<PrintJob> StartJobAsync(IPrinterChannel channel, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ct.ThrowIfCancellationRequested();

        var context = activeJobs.GetOrAdd(channel, static (key, state) =>
        {
            var utcNow = state.Clock.GetUtcNow();
            var printer = key.Printer;
            var jobState = new StreamingPrintJobState(state.Clock);

            var job = new PrintJob(
                Guid.NewGuid(),
                printer.Id,
                printer.DisplayName,
                printer.Protocol,
                printer.WidthInDots,
                printer.HeightInDots,
                utcNow,
                printer.CreatedFromIp,
                printer.ListenTcpPortNumber,
                jobState,
                IsDeleted: false);

            return new PrintJobContext(job, jobState, utcNow);
        }, new OrchestratorState(timeProvider));

        context.LastActivityUtc = timeProvider.GetUtcNow();

        return Task.FromResult(context.Job);
    }

    public async Task StopJobAsync(IPrinterChannel channel, PrintJobCompletionReason reason, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(channel);

        if (!activeJobs.TryRemove(channel, out var context))
        {
            return;
        }

        ct.ThrowIfCancellationRequested();
        await context.State.Complete(reason).ConfigureAwait(false);
    }

    public async Task FeedDataAsync(IPrinterChannel channel, ReadOnlyMemory<byte> data, CancellationToken token)
    {
        if (!activeJobs.TryGetValue(channel, out var context))
        {
            return;
        }

        if (data.Length == 0)
        {
            return;
        }

        token.ThrowIfCancellationRequested();

        await context.State.Feed(data.Span).ConfigureAwait(false);
        context.BytesReceived += data.Length;
        context.LastActivityUtc = timeProvider.GetUtcNow();
    }

    private sealed record OrchestratorState(TimeProvider Clock);

    private sealed class PrintJobContext
    {
        internal PrintJobContext(PrintJob job, IPrintJobState state, DateTimeOffset utcNow)
        {
            Job = job;
            State = state;
            LastActivityUtc = utcNow;
        }

        public PrintJob Job { get; }
        public IPrintJobState State { get; }
        public long BytesReceived { get; set; }
        public DateTimeOffset LastActivityUtc { get; set; }
    }

    private sealed class StreamingPrintJobState : IPrintJobState
    {
        private readonly TimeProvider timeProvider;
        private long totalBytes;
        private bool isCompleted;

        public StreamingPrintJobState(TimeProvider provider)
        {
            timeProvider = provider ?? TimeProvider.System;
            LastReceivedBytes = timeProvider.GetUtcNow();
        }

        public int BytesReceived => (int)Math.Min(totalBytes, int.MaxValue);
        public int SendBytes => 0;
        public DateTimeOffset LastReceivedBytes { get; private set; }
        public bool IsBufferBusy => false;
        public bool HasOverflow => false;
        public Document? Document => null;

        public Task Feed(ReadOnlySpan<byte> data)
        {
            totalBytes += data.Length;
            LastReceivedBytes = timeProvider.GetUtcNow();
            return Task.CompletedTask;
        }

        public Task Complete(PrintJobCompletionReason reason)
        {
            if (isCompleted)
            {
                return Task.CompletedTask;
            }

            isCompleted = true;
            LastReceivedBytes = timeProvider.GetUtcNow();
            return Task.CompletedTask;
        }
    }
}
