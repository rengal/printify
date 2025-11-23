using Printify.Domain.Workspaces;
using Printify.Infrastructure.Persistence.Entities.Workspaces;

namespace Printify.Infrastructure.Mapping;

internal static class WorkspaceEntityMapper
{
    internal static WorkspaceEntity ToEntity(this Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        return new WorkspaceEntity
        {
            Id = workspace.Id,
            OwnerName = workspace.OwnerName,
            Token = workspace.Token,
            CreatedAt = workspace.CreatedAt,
            IsDeleted = workspace.IsDeleted
        };
    }

    internal static Workspace ToDomain(this WorkspaceEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new Workspace(
            entity.Id,
            entity.OwnerName,
            entity.Token,
            entity.CreatedAt,
            entity.CreatedFromIp,
            entity.IsDeleted);
    }
}
