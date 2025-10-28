using Printify.Domain.PrintJobs;

namespace Printify.Application.Printing;

public interface IPrintJobsOrchestrator
{
    Task<PrintJob> StartJobAsync(IPrinterChannel channel, CancellationToken ct);
    Task StopJobAsync(IPrinterChannel channel, PrintJobCompletionReason reason, CancellationToken ct);
    Task FeedDataAsync(IPrinterChannel channel, ReadOnlyMemory<byte> data, CancellationToken token);

    //Task<PrintJobSnapshot?> GetSnapshotAsync(PrintJobId jobId, CancellationToken ct);
    //Task StopAllAsync(CancellationToken ct);
}
