using Microsoft.EntityFrameworkCore;
using Printify.Application.Interfaces;
using Printify.Domain.Workspaces;
using Printify.Infrastructure.Mapping;
using Printify.Infrastructure.Persistence;
    
namespace Printify.Infrastructure.Repositories;

public sealed class WorkspaceRepository(PrintifyDbContext dbContext) : IWorkspaceRepository
{
    public async ValueTask<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(workspace => workspace.Id == id && !workspace.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        return entity?.ToDomain();
    }

    public async ValueTask<Workspace?> GetByTokenAsync(string token, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var entity = await dbContext.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(workspace => workspace.Token == token && !workspace.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        return entity?.ToDomain();
    }

    public async Task AddAsync(Workspace workspace, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var entity = workspace.ToEntity();
        await dbContext.Workspaces.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Workspace workspace, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var entity = await dbContext.Workspaces
            .FirstOrDefaultAsync(w => w.Id == workspace.Id && !w.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            throw new InvalidOperationException($"Workspace with id {workspace.Id} not found");
        }

        entity.Name = workspace.Name;
        entity.DocumentRetentionDays = workspace.DocumentRetentionDays;

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Workspaces
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            throw new InvalidOperationException($"Workspace with id {id} not found");
        }

        // Hard delete: Remove workspace and all related entities
        // Note: Due to FK constraints, we need to delete in the correct order:
        // 1. Documents (related to printers)
        // 2. Printers (related to workspace)
        // 3. Workspace

        // Delete all documents for printers in this workspace
        var printerIds = await dbContext.Printers
            .Where(p => p.OwnerWorkspaceId == id)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (printerIds.Count > 0)
        {
            var documentsToDelete = dbContext.Documents
                .Where(d => printerIds.Contains(d.PrinterId));

            dbContext.Documents.RemoveRange(documentsToDelete);
        }

        // Delete all printers in this workspace
        var printersToDelete = await dbContext.Printers
            .Where(p => p.OwnerWorkspaceId == id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (printersToDelete.Count > 0)
        {
            dbContext.Printers.RemoveRange(printersToDelete);
        }

        // Delete the workspace
        dbContext.Workspaces.Remove(entity);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
