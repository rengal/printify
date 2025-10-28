using Printify.Domain.PrintJobs;

namespace Printify.Application.Interfaces;

public interface IPrinterJobRepository
{
    ValueTask<PrintJob?> GetByIdAsync(Guid id, CancellationToken ct);
    ValueTask<Guid> AddAsync(PrintJob job, CancellationToken ct);
    Task UpdateAsync(PrintJob job, CancellationToken ct);
}
