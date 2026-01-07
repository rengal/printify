using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Domain.Workspaces;

namespace Printify.Application.Features.Auth.Login;

public sealed class LoginHandler(IWorkspaceRepository workspaceRepository)
    : IRequestHandler<LoginCommand, Workspace>
{
    public async Task<Workspace> Handle(IReceiveContext<LoginCommand> context, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        var workspace = await workspaceRepository.GetByTokenAsync(request.Token, ct);
        if (workspace == null)
            throw new AuthenticationFailedException("Workspace with specified token not found");
        return workspace;
    }
}

