using Mediator.Net.Contracts;
using Printify.Domain.Workspaces;

namespace Printify.Application.Features.Workspaces.GetWorkspaceById;

public sealed record GetWorkspaceByTokenQuery(Guid WorkspaceId) : IRequest;

