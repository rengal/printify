using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Printify.Application.Interfaces;
using Printify.Domain.PrintJobs;
using Printify.Infrastructure.Persistence;
using Printify.Infrastructure.Persistence.Entities.PrinterJobs;

namespace Printify.Infrastructure.Repositories;

public sealed class PrinterJobRepository(PrintifyDbContext context) : IPrinterJobRepository
{
    public async ValueTask<PrintJob?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await context.PrinterJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(job => job.Id == id, ct)
            .ConfigureAwait(false);

        return entity?.ToDomain();
    }

    public async ValueTask<Guid> AddAsync(PrintJob job, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(job);

        var entity = PrinterJobEntityMapper.FromDomain(job);
        await context.PrinterJobs.AddAsync(entity, ct).ConfigureAwait(false);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return entity.Id;
    }

    public async Task UpdateAsync(PrintJob job, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(job);

        var entity = await context.PrinterJobs.FirstOrDefaultAsync(x => x.Id == job.Id, ct).ConfigureAwait(false);
        if (entity == null)
        {
            throw new InvalidOperationException($"Printer job {job.Id} was not found.");
        }

        PrinterJobEntityMapper.Update(entity, job);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

internal static class PrinterJobEntityMapper
{
    public static PrintJob ToDomain(this PrinterJobEntity entity)
    {
        return new PrintJob(
            entity.Id,
            entity.PrinterId,
            entity.DisplayName,
            entity.Protocol,
            entity.WidthInDots,
            entity.HeightInDots,
            entity.CreatedAt,
            entity.CreatedFromIp,
            entity.ListenTcpPortNumber,
            IsDeleted: entity.IsDeleted);
    }

    public static PrinterJobEntity FromDomain(PrintJob job)
    {
        return new PrinterJobEntity
        {
            Id = job.Id,
            CreatedAt = job.CreatedAt,
            IsDeleted = job.IsDeleted,
            PrinterId = job.PrinterId,
            DisplayName = job.DisplayName,
            Protocol = job.Protocol,
            WidthInDots = job.WidthInDots,
            HeightInDots = job.HeightInDots,
            CreatedFromIp = job.CreatedFromIp,
            ListenTcpPortNumber = job.ListenTcpPortNumber
        };
    }

    public static void Update(PrinterJobEntity entity, PrintJob job)
    {
        entity.DisplayName = job.DisplayName;
        entity.Protocol = job.Protocol;
        entity.WidthInDots = job.WidthInDots;
        entity.HeightInDots = job.HeightInDots;
        entity.ListenTcpPortNumber = job.ListenTcpPortNumber;
        entity.IsDeleted = job.IsDeleted;
    }
}
