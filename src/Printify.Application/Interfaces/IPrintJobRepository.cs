using Printify.Domain.PrintJobs;

namespace Printify.Application.Interfaces;

public interface IPrintJobRepository
{
    ValueTask<PrintJob?> GetByIdAsync(Guid id, CancellationToken ct);
    ValueTask AddAsync(PrintJob job, CancellationToken ct);
}
