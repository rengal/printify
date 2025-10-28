using System;
using System.Threading;
using System.Threading.Tasks;
using Printify.Domain.Printers;
using Printify.Domain.PrintJobs;

namespace Printify.Application.Printing;

public interface IPrintJobsOrchestrator
{
    Task<PrintJob> StartJobAsync(Printer printer, IPrinterChannel channel, CancellationToken ct);
    //Task<PrintJobSnapshot?> GetSnapshotAsync(PrintJobId jobId, CancellationToken ct);
    Task StopJobAsync(PrintJob job, CancellationToken ct);
    Task StopAllAsync(CancellationToken ct);
}
