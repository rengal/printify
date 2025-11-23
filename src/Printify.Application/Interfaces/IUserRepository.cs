using Printify.Domain.Workspaces;

namespace Printify.Application.Interfaces;

public interface IWorkspaceRepository
{
    ValueTask<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    ValueTask<Workspace?> GetByTokenAsync(string token, CancellationToken cancellationToken);
    Task AddAsync(Workspace workspace, CancellationToken cancellationToken);
}
