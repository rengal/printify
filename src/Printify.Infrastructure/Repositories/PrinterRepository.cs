using System.Net;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Printify.Application.Interfaces;
using Printify.Domain.Printers;
using Printify.Infrastructure.Mapping;
using Printify.Infrastructure.Persistence;
using Printify.Infrastructure.Persistence.Entities.Printers;

namespace Printify.Infrastructure.Repositories;

public sealed class PrinterRepository(PrintifyDbContext dbContext) : IPrinterRepository
{
    public async ValueTask<Printer?> GetByIdAsync(Guid id, Guid? ownerUserId, Guid? ownerSessionId, CancellationToken ct)
    {
        var query = dbContext.Printers
            .AsNoTracking()
            .Where(printer => printer.Id == id && !printer.IsDeleted);

        query = ApplyOwnershipFilter(query, ownerUserId, ownerSessionId);

        var entity = await query.FirstOrDefaultAsync(ct).ConfigureAwait(false);

        return entity?.ToDomain();
    }

    public async ValueTask<IReadOnlyList<Printer>> ListAccessibleAsync(Guid? ownerUserId, Guid? ownerSessionId, CancellationToken ct)
    {
        var query = dbContext.Printers
            .AsNoTracking()
            .Where(printer => !printer.IsDeleted);

        query = ApplyOwnershipFilter(query, ownerUserId, ownerSessionId);

        var entities = await query.ToListAsync(ct).ConfigureAwait(false);

        return entities.Select(entity => entity.ToDomain()).ToList();
    }

    public async ValueTask<Guid> AddAsync(Printer printer, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(printer);

        var entity = printer.ToEntity();
        await dbContext.Printers.AddAsync(entity, ct).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        return printer.Id;
    }

    public async Task UpdateAsync(Printer printer, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(printer);

        var entity = await dbContext.Printers
            .FirstOrDefaultAsync(p => p.Id == printer.Id && !p.IsDeleted, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            throw new InvalidOperationException($"Printer {printer.Id} does not exist.");
        }

        entity.OwnerUserId = printer.OwnerUserId;
        entity.OwnerAnonymousSessionId = printer.OwnerAnonymousSessionId;
        entity.DisplayName = printer.DisplayName;
        entity.Protocol = printer.Protocol;
        entity.WidthInDots = printer.WidthInDots;
        entity.HeightInDots = printer.HeightInDots;
        entity.ListenTcpPortNumber = printer.ListenTcpPortNumber;
        entity.CreatedFromIp = printer.CreatedFromIp;
        entity.IsDeleted = printer.IsDeleted;

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Printer printer, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(printer);

        var entity = await dbContext.Printers
            .FirstOrDefaultAsync(p => p.Id == printer.Id && !p.IsDeleted, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return;
        }

        if (!entity.IsDeleted)
        {
            entity.IsDeleted = true;
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    public ValueTask<int> GetFreeTcpPortNumber(CancellationToken ct)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return ValueTask.FromResult(port);
    }

    private static IQueryable<PrinterEntity> ApplyOwnershipFilter(
        IQueryable<PrinterEntity> source,
        Guid? ownerUserId,
        Guid? ownerSessionId)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (ownerUserId is null && ownerSessionId is null)
        {
            return source.Where(_ => false);
        }

        return source.Where(printer =>
            (ownerUserId.HasValue && printer.OwnerUserId == ownerUserId)
            || (ownerSessionId.HasValue && printer.OwnerAnonymousSessionId == ownerSessionId));
    }
}

