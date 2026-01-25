using Printify.Domain.PrintJobs;

namespace Printify.Application.Interfaces;

public interface IPrintJobRepository
{
    ValueTask AddAsync(PrintJob job, CancellationToken ct);
}
