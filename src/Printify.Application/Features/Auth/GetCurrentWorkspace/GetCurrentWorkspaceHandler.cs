using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Domain.Workspaces;

namespace Printify.Application.Features.Auth.GetCurrentWorkspace;

public sealed class GetCurrentWorkspaceHandler(IWorkspaceRepository workspaceRepository)
    : IRequestHandler<GetCurrentWorkspaceCommand, Workspace>
{
    public async Task<Workspace> Handle(IReceiveContext<GetCurrentWorkspaceCommand> context, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        if (!request.Context.AuthValid)
            throw new AuthenticationFailedException("Authentication failed");
        var workspaceId = request.Context.WorkspaceId ?? throw new BadRequestException("WorkspaceId must not be empty");
        var workspace = await workspaceRepository.GetByIdAsync(workspaceId, ct) ??
                        throw new AuthenticationFailedException("Workspace not found");
        return workspace;
    }
}

