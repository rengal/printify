using Printify.Domain.PrintJobs;

namespace Printify.Application.Printing;

public interface IPrintJobSessionsOrchestrator
{
    Task<IPrintJobSession> StartSessionAsync(IPrinterChannel channel, CancellationToken ct);
    Task FeedAsync(IPrinterChannel channel, ReadOnlyMemory<byte> data, CancellationToken ct);
    Task CompleteAsync(IPrinterChannel channel, PrintJobCompletionReason reason, CancellationToken ct);
}
