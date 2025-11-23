using MediatR;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Domain.Workspaces;

namespace Printify.Application.Features.Auth.GetCurrentWorkspace;

public sealed class GetCurrentWorkspaceHandler(IWorkspaceRepository workspaceRepository)
    : IRequestHandler<GetCurrentWorkspaceCommand, Workspace>
{
    public async Task<Workspace> Handle(GetCurrentWorkspaceCommand request, CancellationToken ct)
    {
        var workspaceId = request.Context.WorkspaceId ?? throw new BadRequestException("WorkspaceId must not be empty");
        var workspace = await workspaceRepository.GetByIdAsync(workspaceId, ct) ??
                        throw new AuthenticationFailedException("Workspace not found");
        return workspace;
    }
}
