using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.PrintJobs;

namespace Printify.Infrastructure.Printing;

/// <summary>
/// Coordinates live print job sessions per printer channel.
/// </summary>
public sealed class PrintJobSessionsOrchestrator(
    IPrintJobSessionFactory printJobSessionFactory,
    IServiceScopeFactory scopeFactory)
    : IPrintJobSessionsOrchestrator
{
    private readonly ConcurrentDictionary<IPrinterChannel, IPrintJobSession> jobSessions = new();

    public async Task<IPrintJobSession> StartSessionAsync(IPrinterChannel channel, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(channel);
        ct.ThrowIfCancellationRequested();

        var printer = channel.Printer;

        var printJob = new PrintJob(Guid.NewGuid(), printer, DateTimeOffset.Now, channel.ClientAddress);
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var printJobRepository = scope.ServiceProvider.GetRequiredService<IPrintJobRepository>();
            await printJobRepository.AddAsync(printJob, ct).ConfigureAwait(false);
        }

        var jobSession = await printJobSessionFactory.Create(printJob, channel, ct);
        jobSessions[channel] = jobSession;
        return jobSession;
    }

    public async Task FeedAsync(IPrinterChannel channel, ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        if (!jobSessions.TryGetValue(channel, out var session) || data.Length == 0)
            return;

        ct.ThrowIfCancellationRequested();
        await session.Feed(data, ct);
    }

    public async Task CompleteAsync(IPrinterChannel channel, PrintJobCompletionReason reason, CancellationToken ct)
    {
        if (!jobSessions.TryRemove(channel, out var session))
            return;
        await session.Complete(reason).ConfigureAwait(false);
    }
}
