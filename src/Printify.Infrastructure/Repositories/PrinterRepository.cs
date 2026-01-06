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

    public async ValueTask<IReadOnlyList<PrinterSidebarSnapshot>> ListForSidebarAsync(Guid workspaceId, CancellationToken ct)
    {
        var printers = await dbContext.Printers
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.OwnerWorkspaceId == workspaceId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (printers.Count == 0)
        {
            return [];
        }

        return printers
            .Select(printer =>
                new PrinterSidebarSnapshot(printer.ToDomain()))
            .ToList();
    }

    public async ValueTask AddAsync(Printer printer, PrinterSettings settings, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentNullException.ThrowIfNull(settings);

        var entity = printer.ToEntity(settings);
        await dbContext.Printers.AddAsync(entity, ct).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Printer printer, PrinterSettings settings, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentNullException.ThrowIfNull(settings);

        var entity = await dbContext.Printers
            .FirstOrDefaultAsync(p => p.Id == printer.Id && !p.IsDeleted, ct)
            .ConfigureAwait(false);

        if (entity is null)
            throw new InvalidOperationException($"Printer {printer.Id} does not exist.");

        printer.MapToEntity(settings, entity);

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SetPinnedAsync(Guid id, bool isPinned, CancellationToken ct)
    {
        var entity = await dbContext.Printers
            .Where(p => p.Id == id && !p.IsDeleted)
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
            var operationalFlags = await dbContext.PrinterOperationalFlags
                .FirstOrDefaultAsync(status => status.PrinterId == printer.Id, ct)
                .ConfigureAwait(false);
            if (operationalFlags is not null)
            {
                dbContext.PrinterOperationalFlags.Remove(operationalFlags);
            }
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

    public async Task ClearLastDocumentReceivedAtAsync(Guid id, CancellationToken ct)
    {
        var entity = await dbContext.Printers
            .Where(p => p.Id == id && !p.IsDeleted)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return;
        }

        entity.LastDocumentReceivedAt = null;
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public ValueTask<int> GetFreeTcpPortNumber(CancellationToken ct)
    {
        // Choose next sequential port based on existing printers; fall back to 9101 when none exist.
        var maxPort = dbContext.Printers
            .AsNoTracking()
            .Select(p => (int?)p.Settings.ListenTcpPortNumber)
            .Max() ?? 9100; // so that first printer becomes 9101

        var nextPort = maxPort + 1;

        return ValueTask.FromResult(nextPort);
    }

    public async ValueTask<PrinterOperationalFlags?> GetOperationalFlagsAsync(Guid printerId, CancellationToken ct)
    {
        var entity = await dbContext.PrinterOperationalFlags
            .AsNoTracking()
            .FirstOrDefaultAsync(status => status.PrinterId == printerId, ct)
            .ConfigureAwait(false);

        return entity?.ToDomain();
    }

    public async ValueTask<PrinterSettings?> GetSettingsAsync(Guid printerId, CancellationToken ct)
    {
        var entity = await dbContext.Printers
            .AsNoTracking()
            .Where(p => p.Id == printerId && !p.IsDeleted)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return entity?.ToSettings();
    }

    public async ValueTask<IReadOnlyDictionary<Guid, PrinterSettings>> ListSettingsAsync(
        Guid workspaceId,
        CancellationToken ct)
    {
        var printers = await dbContext.Printers
            .AsNoTracking()
            .Where(p => p.OwnerWorkspaceId == workspaceId && !p.IsDeleted)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return printers
            .ToDictionary(printer => printer.Id, printer => printer.ToSettings());
    }

    public async ValueTask<IReadOnlyDictionary<Guid, PrinterOperationalFlags>> ListOperationalFlagsAsync(
        Guid workspaceId,
        CancellationToken ct)
    {
        var statuses = await (from status in dbContext.PrinterOperationalFlags.AsNoTracking()
                              join printer in dbContext.Printers.AsNoTracking()
                                  on status.PrinterId equals printer.Id
                              where printer.OwnerWorkspaceId == workspaceId && !printer.IsDeleted
                              select status)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return statuses
            .Select(status => status.ToDomain())
            .ToDictionary(status => status.PrinterId);
    }

    public async Task UpsertOperationalFlagsAsync(PrinterOperationalFlagsUpdate update, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(update);

        var printerEntity = await dbContext.Printers
            .Where(p => p.Id == update.PrinterId && !p.IsDeleted)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (printerEntity is null)
        {
            throw new PrinterNotFoundException(update.PrinterId);
        }

        var entity = await dbContext.PrinterOperationalFlags
            .FirstOrDefaultAsync(existing => existing.PrinterId == update.PrinterId, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            entity = new PrinterOperationalFlagsEntity();
            entity.PrinterId = update.PrinterId;
            entity.TargetState = (update.TargetState ?? PrinterTargetState.Started).ToString();
            entity.UpdatedAt = update.UpdatedAt;
            entity.IsCoverOpen = update.IsCoverOpen ?? false;
            entity.IsPaperOut = update.IsPaperOut ?? false;
            entity.IsOffline = update.IsOffline ?? false;
            entity.HasError = update.HasError ?? false;
            entity.IsPaperNearEnd = update.IsPaperNearEnd ?? false;
            await dbContext.PrinterOperationalFlags.AddAsync(entity, ct).ConfigureAwait(false);
        }
        else
        {
            update.MapToEntity(entity);
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

}
