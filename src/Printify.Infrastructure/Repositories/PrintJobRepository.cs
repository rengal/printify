using Microsoft.EntityFrameworkCore;
using Printify.Application.Interfaces;
using Printify.Domain.PrintJobs;
using Printify.Infrastructure.Mapping;
using Printify.Infrastructure.Persistence;

namespace Printify.Infrastructure.Repositories;

public sealed class PrintJobRepository(PrintifyDbContext context) : IPrintJobRepository
{
    // public async ValueTask<PrintJob?> GetByIdAsync(Guid id, CancellationToken ct)
    // {
    //     var entity = await context.PrintJobs
    //         .AsNoTracking()
    //         .FirstOrDefaultAsync(job => job.Id == id, ct)
    //         .ConfigureAwait(false);
    //
    //     return entity?.ToDomain();
    // }

    public async ValueTask AddAsync(PrintJob job, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(job);

        var entity = job.ToEntity();
        await context.PrintJobs.AddAsync(entity, ct).ConfigureAwait(false);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
