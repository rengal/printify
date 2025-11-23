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
}
