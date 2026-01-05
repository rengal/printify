using Microsoft.EntityFrameworkCore;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Domain.Mapping;
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
            var realtimeStatus = await dbContext.PrinterRealtimeStatuses
                .FirstOrDefaultAsync(status => status.PrinterId == printer.Id, ct)
                .ConfigureAwait(false);
            if (realtimeStatus is not null)
            {
                dbContext.PrinterRealtimeStatuses.Remove(realtimeStatus);
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

    public ValueTask<int> GetFreeTcpPortNumber(CancellationToken ct)
    {
        // Choose next sequential port based on existing printers; fall back to 9101 when none exist.
        var maxPort = dbContext.Printers
            .AsNoTracking()
            .Select(p => (int?)p.ListenTcpPortNumber)
            .Max() ?? 9100; // so that first printer becomes 9101

        var nextPort = maxPort + 1;

        return ValueTask.FromResult(nextPort);
    }

    public async ValueTask<PrinterRealtimeStatus?> GetRealtimeStatusAsync(Guid printerId, CancellationToken ct)
    {
        var entity = await dbContext.PrinterRealtimeStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(status => status.PrinterId == printerId, ct)
            .ConfigureAwait(false);

        return entity?.ToDomain();
    }

    public async ValueTask<IReadOnlyDictionary<Guid, PrinterRealtimeStatus>> ListRealtimeStatusesAsync(
        Guid workspaceId,
        CancellationToken ct)
    {
        var statuses = await (from status in dbContext.PrinterRealtimeStatuses.AsNoTracking()
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

    public async Task UpsertRealtimeStatusAsync(PrinterRealtimeStatusUpdate update, CancellationToken ct)
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

        var entity = await dbContext.PrinterRealtimeStatuses
            .FirstOrDefaultAsync(existing => existing.PrinterId == update.PrinterId, ct)
            .ConfigureAwait(false);

        // Default to Started to preserve legacy target-state behavior when no snapshot exists.
        var effectiveTargetState = update.TargetState
            ?? (entity is null ? PrinterTargetState.Started : ParseTargetState(entity.TargetState));

        // Update printer's target status only when the caller sets it or when creating a new snapshot.
        if (update.TargetState is not null || entity is null)
        {
            var desiredTarget = DomainMapper.ToString(effectiveTargetState);
            if (!string.Equals(printerEntity.TargetStatus, desiredTarget, StringComparison.Ordinal))
            {
                printerEntity.TargetStatus = desiredTarget;
            }
        }

        if (entity is null)
        {
            entity = new PrinterRealtimeStatusEntity();
            entity.PrinterId = update.PrinterId;
            entity.TargetState = effectiveTargetState.ToString();
            entity.UpdatedAt = update.UpdatedAt;
            entity.BufferedBytes = update.BufferedBytes ?? 0;
            entity.IsCoverOpen = update.IsCoverOpen ?? false;
            entity.IsPaperOut = update.IsPaperOut ?? false;
            entity.IsOffline = update.IsOffline ?? false;
            entity.HasError = update.HasError ?? false;
            entity.IsPaperNearEnd = update.IsPaperNearEnd ?? false;
            entity.Drawer1State = (update.Drawer1State ?? DrawerState.Closed).ToString();
            entity.Drawer2State = (update.Drawer2State ?? DrawerState.Closed).ToString();
            await dbContext.PrinterRealtimeStatuses.AddAsync(entity, ct).ConfigureAwait(false);
        }
        else
        {
            // Persist only provided fields to avoid overwriting stored values with defaults.
            if (update.TargetState is not null)
            {
                entity.TargetState = update.TargetState.Value.ToString();
            }

            entity.UpdatedAt = update.UpdatedAt;
            // Update only provided fields to avoid overwriting stored values with defaults.
            if (update.BufferedBytes is not null)
            {
                entity.BufferedBytes = update.BufferedBytes.Value;
            }

            if (update.IsCoverOpen is not null)
            {
                entity.IsCoverOpen = update.IsCoverOpen.Value;
            }

            if (update.IsPaperOut is not null)
            {
                entity.IsPaperOut = update.IsPaperOut.Value;
            }

            if (update.IsOffline is not null)
            {
                entity.IsOffline = update.IsOffline.Value;
            }

            if (update.HasError is not null)
            {
                entity.HasError = update.HasError.Value;
            }

            if (update.IsPaperNearEnd is not null)
            {
                entity.IsPaperNearEnd = update.IsPaperNearEnd.Value;
            }

            if (update.Drawer1State is not null)
            {
                entity.Drawer1State = update.Drawer1State.Value.ToString();
            }

            if (update.Drawer2State is not null)
            {
                entity.Drawer2State = update.Drawer2State.Value.ToString();
            }
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static PrinterTargetState ParseTargetState(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return PrinterTargetState.Stopped;
        }

        return Enum.TryParse<PrinterTargetState>(value, true, out var parsed)
            ? parsed
            : PrinterTargetState.Stopped;
    }
}
