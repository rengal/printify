using System;
using System.Threading;
using System.Threading.Tasks;

namespace Printify.Application.Printing;

public interface IPrintJobsOrchestrator
{
    Task<PrintJobId> StartJobAsync(Guid printerId, IPrinterChannel channel, CancellationToken cancellationToken);
    Task<PrintJobSnapshot?> GetSnapshotAsync(PrintJobId jobId, CancellationToken cancellationToken);
    Task StopJobAsync(PrintJobId jobId, CancellationToken cancellationToken);
    Task StopAllAsync(CancellationToken cancellationToken);
}
