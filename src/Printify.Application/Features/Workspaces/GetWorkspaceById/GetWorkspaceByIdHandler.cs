using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Interfaces;
using Printify.Domain.Workspaces;

namespace Printify.Application.Features.Workspaces.GetWorkspaceById;

public sealed class GetWorkspaceByTokenHandler(IWorkspaceRepository workspaceRepository)
    : IRequestHandler<GetWorkspaceByTokenQuery, Workspace?>
{
    public async Task<Workspace?> Handle(IReceiveContext<GetWorkspaceByTokenQuery> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        ArgumentNullException.ThrowIfNull(request);

        var workspace = await workspaceRepository.GetByIdAsync(request.WorkspaceId, cancellationToken)
            .ConfigureAwait(false);

        return workspace;
    }
}
