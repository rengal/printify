using MediatR;
using Printify.Application.Interfaces;
using Printify.Domain.Requests;
using Printify.Domain.Workspaces;

namespace Printify.Application.Features.Auth.Login;

public record LoginCommand(
    RequestContext Context,
    string Token) : IRequest<Workspace>, ITransactionalRequest;
