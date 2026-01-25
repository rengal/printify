using Printify.Domain.Workspaces;
using Printify.Infrastructure.Persistence.Entities.Workspaces;

namespace Printify.Infrastructure.Mapping;

/// <summary>
/// Bidirectional mapper between Workspace domain and persistence entities.
/// </summary>
internal static class WorkspaceMapper
{
    internal static WorkspaceEntity ToEntity(this Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        return new WorkspaceEntity
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Token = workspace.Token,
            CreatedAt = workspace.CreatedAt,
            CreatedFromIp = workspace.CreatedFromIp,
            DocumentRetentionDays = workspace.DocumentRetentionDays,
            IsDeleted = workspace.IsDeleted
        };
    }

    internal static Workspace ToDomain(this WorkspaceEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new Workspace(
            entity.Id,
            entity.Name,
            entity.Token,
            entity.CreatedAt,
            entity.CreatedFromIp,
            entity.DocumentRetentionDays,
            entity.IsDeleted);
    }
}
