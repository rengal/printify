using System.Collections.Concurrent;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Documents;
using Printify.Domain.PrintJobs;

namespace Printify.Infrastructure.Printing;

/// <summary>
/// Coordinates live print job sessions per printer channel.
/// </summary>
public sealed class PrintJobSessionsOrchestrator(IPrintJobRepository printJobRepository, TimeProvider clock) : IPrintJobSessionsOrchestrator
{
    private readonly TimeProvider timeProvider = clock ?? TimeProvider.System;
    private readonly ConcurrentDictionary<IPrinterChannel, IPrintJobSession> jobSessions = new();

    public Task<IPrintJobSession> StartSessionAsync(IPrinterChannel channel, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ct.ThrowIfCancellationRequested();

        var printer = channel.Printer;

        var printJob = new PrintJob(Guid.NewGuid(), printer, DateTimeOffset.Now, channel.ClientAddress);
        await printJobRepository.AddAsync(printJob, ct);

        var jobSession = new PrintJobSession(printJob, channel);
        jobSessions[channel] = jobSession;
        return Task.FromResult(jobSession);
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
}
