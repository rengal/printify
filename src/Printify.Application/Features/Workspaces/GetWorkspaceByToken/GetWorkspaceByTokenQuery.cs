using Mediator.Net.Contracts;
using Printify.Domain.Workspaces;

namespace Printify.Application.Features.Workspaces.GetWorkspaceByToken;

public sealed record GetWorkspaceByTokenQuery(string Token) : IRequest;

