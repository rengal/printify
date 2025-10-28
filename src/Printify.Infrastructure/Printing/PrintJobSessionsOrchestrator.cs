using System.Collections.Concurrent;
using Printify.Application.Printing;
using Printify.Domain.Documents;
using Printify.Domain.PrintJobs;

namespace Printify.Infrastructure.Printing;

/// <summary>
/// Coordinates live print job sessions per printer channel.
/// </summary>
public sealed class PrintJobSessionsOrchestrator(TimeProvider clock) : IPrintJobSessionsOrchestrator
{
    private readonly TimeProvider timeProvider = clock ?? TimeProvider.System;
    private readonly ConcurrentDictionary<IPrinterChannel, IPrintJobSession> jobSessions = new();

    public Task<IPrintJobSession> StartSessionAsync(IPrinterChannel channel, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(jobSessions.GetOrAdd(channel, static (key, state) =>
        {
            var now = state.Clock.GetUtcNow();
            var printer = key.Printer;
            var job = new PrintJob(
                Guid.NewGuid(),
                printer.Id,
                printer.DisplayName,
                printer.Protocol,
                printer.WidthInDots,
                printer.HeightInDots,
                now,
                printer.CreatedFromIp,
                printer.ListenTcpPortNumber,
                IsDeleted: false);

            return new StreamingPrintJobSession(job, state.Clock);
        }, new SessionFactoryState(timeProvider)));
    }

    public async Task FeedAsync(IPrinterChannel channel, ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        if (!jobSessions.TryGetValue(channel, out var session))
        {
            return;
        }

        if (data.Length == 0)
        {
            return;
        }

        ct.ThrowIfCancellationRequested();
        await session.Feed(data.Span).ConfigureAwait(false);
    }

    public async Task CompleteAsync(IPrinterChannel channel, PrintJobCompletionReason reason, CancellationToken ct)
    {
        if (!jobSessions.TryRemove(channel, out var session))
        {
            return;
        }

        ct.ThrowIfCancellationRequested();
        await session.Complete(reason).ConfigureAwait(false);
    }

    private sealed record SessionFactoryState(TimeProvider Clock);

    private sealed class StreamingPrintJobSession : IPrintJobSession
    {
        private readonly TimeProvider timeProvider;
        private long totalBytes;
        private bool isCompleted;

        public StreamingPrintJobSession(PrintJob job, TimeProvider provider)
        {
            Job = job;
            timeProvider = provider ?? TimeProvider.System;
            LastReceivedBytes = timeProvider.GetUtcNow();
        }

        public PrintJob Job { get; }
        public int BytesReceived => (int)Math.Min(totalBytes, int.MaxValue);
        public int SendBytes => 0;
        public DateTimeOffset LastReceivedBytes { get; private set; }
        public bool IsBufferBusy => false;
        public bool HasOverflow => false;
        public Document? Document => null;

        public Task Feed(ReadOnlySpan<byte> data)
        {
            if (data.Length == 0)
            {
                return Task.CompletedTask;
            }

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
