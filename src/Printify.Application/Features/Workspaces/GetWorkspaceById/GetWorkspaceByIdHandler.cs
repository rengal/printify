using MediatR;
using Printify.Application.Interfaces;
using Printify.Domain.Workspaces;

namespace Printify.Application.Features.Workspaces.GetWorkspaceById;

public sealed class GetWorkspaceByTokenHandler(IWorkspaceRepository workspaceRepository)
    : IRequestHandler<GetWorkspaceByTokenQuery, Workspace?>
{
    public async Task<Workspace?> Handle(GetWorkspaceByTokenQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var workspace = await workspaceRepository.GetByIdAsync(request.WorkspaceId, cancellationToken)
            .ConfigureAwait(false);

        return workspace;
    }
}