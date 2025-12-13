using System.Net;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Domain.Printers;
using Printify.Infrastructure.Mapping;
using Printify.Infrastructure.Persistence;

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

    public async ValueTask<Printer?> GetByIdAsync(Guid id, Guid? workspaceId, CancellationToken ct)
    {
        var entity = await dbContext.Printers
            .AsNoTracking()
            .Where(p => p.Id == id && !p.IsDeleted)
            .Where(p => workspaceId == null || p.OwnerWorkspaceId == workspaceId)
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

    public async ValueTask<IReadOnlyList<Printer>> ListOwnedAsync(Guid? workspaceId, CancellationToken ct)
    {
        if (workspaceId is null)
            return [];

        return await dbContext.Printers
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.OwnerWorkspaceId == workspaceId)
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
            throw new InvalidOperationException($"Printer {printer.Id} does not exist.");

        printer.MapToEntity(entity);

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SetPinnedAsync(Guid id, Guid? workspaceId, bool isPinned, CancellationToken ct)
    {
        var entity = await dbContext.Printers
            .Where(p => p.Id == id && !p.IsDeleted)
            .Where(p => workspaceId == null || p.OwnerWorkspaceId == workspaceId)
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

    public async Task SetLastDocumentReceivedAtAsync(Guid id, DateTimeOffset timestamp, CancellationToken ct)
    {
        var entity = await dbContext.Printers
            .Where(p => p.Id == id && !p.IsDeleted)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return;
        }

        entity.LastDocumentReceivedAt = timestamp;
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public ValueTask<int> GetFreeTcpPortNumber(CancellationToken ct)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return ValueTask.FromResult(port);
    }
}
