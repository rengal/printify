using MediatR;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Domain.Workspaces;

namespace Printify.Application.Features.Auth.Login;

public sealed class LoginHandler(IWorkspaceRepository workspaceRepository)
    : IRequestHandler<LoginCommand, Workspace>
{
    public async Task<Workspace> Handle(LoginCommand request, CancellationToken ct)
    {
        var workspace = await workspaceRepository.GetByTokenAsync(request.Token, ct);
        if (workspace == null)
            throw new AuthenticationFailedException("Workspace with specified token not found");
        return workspace;
    }
}
