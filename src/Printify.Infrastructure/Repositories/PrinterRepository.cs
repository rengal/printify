using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Domain.Printers;
using Printify.Infrastructure.Mapping;
using Printify.Infrastructure.Persistence;
using Printify.Infrastructure.Persistence.Entities.Printers;

namespace Printify.Infrastructure.Repositories;

public sealed class PrinterRepository(PrintifyDbContext dbContext) : IPrinterRepository
{
    public async ValueTask<Printer?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await dbContext.Printers
            .AsNoTracking()
            .Where(printer => printer.Id == id && !printer.IsDeleted)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return entity?.ToDomain();
    }


    public async ValueTask<Printer?> GetByIdAsync(Guid id, Guid? ownerUserId, Guid? ownerSessionId, CancellationToken ct)
    {
        var entity = await dbContext.Printers
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Where(OwnershipPredicate(ownerUserId, ownerSessionId))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        return entity?.ToDomain();
    }

    public async ValueTask<IReadOnlyList<Printer>> ListAllAsync(CancellationToken ct)
    {
        return await dbContext.Printers
            .AsNoTracking()
            .Where(printer => !printer.IsDeleted)
            .Select(entity => entity.ToDomain())
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async ValueTask<IReadOnlyList<Printer>> ListOwnedAsync(Guid? ownerUserId, Guid? ownerSessionId, CancellationToken ct)
    {
        if (ownerUserId is null && ownerSessionId is null)
            return [];

        return await dbContext.Printers
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Where(OwnershipPredicate(ownerUserId, ownerSessionId))
            .Select(e => e.ToDomain())
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async ValueTask AddAsync(Printer printer, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(printer);

        var entity = printer.ToEntity();
        await dbContext.Printers.AddAsync(entity, ct).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
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
        entity.IsPinned = printer.IsPinned;
        entity.IsDeleted = printer.IsDeleted;

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SetPinnedAsync(Guid id, Guid? ownerUserId, Guid? ownerSessionId, bool isPinned,
        CancellationToken ct)
    {
        var entity = await dbContext.Printers
            .Where(p => p.Id == id && !p.IsDeleted)
            .Where(OwnershipPredicate(ownerUserId, ownerSessionId))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (entity is null)
            throw new PrinterNotFoundException(id);

        if (entity.IsPinned == isPinned)
            return;

        entity.IsPinned = isPinned;
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

    private static Expression<Func<PrinterEntity, bool>> OwnershipPredicate(Guid? ownerUserId, Guid? ownerSessionId)
    {
        if (ownerUserId is null && ownerSessionId is null)
            return _ => false;

        if (ownerUserId is not null && ownerSessionId is null)
            return p => p.OwnerUserId == ownerUserId;

        if (ownerUserId is null && ownerSessionId is not null)
            return p => p.OwnerAnonymousSessionId == ownerSessionId;

        return p => p.OwnerUserId == ownerUserId || p.OwnerAnonymousSessionId == ownerSessionId;
    }
}
