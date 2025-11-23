using MediatR;
using Printify.Application.Interfaces;
using Printify.Domain.Workspaces;

namespace Printify.Application.Features.Workspaces.GetWorkspaceByToken;

public sealed class GetWorkspaceByTokenHandler(IWorkspaceRepository workspaceRepository)
    : IRequestHandler<GetWorkspaceByTokenQuery, Workspace?>
{
    public async Task<Workspace?> Handle(GetWorkspaceByTokenQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var workspace = await workspaceRepository.GetByTokenAsync(request.Token, cancellationToken)
            .ConfigureAwait(false);

        return workspace;
    }
}

