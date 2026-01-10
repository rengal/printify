using Mediator.Net.Contracts;
using Printify.Application.Interfaces;
using Printify.Domain.Requests;
using Printify.Domain.Workspaces;

namespace Printify.Application.Features.Workspaces.CreateWorkspace;

public sealed record CreateWorkspaceCommand(
    RequestContext Context,
    Guid WorkspaceId,
    string WorkspaceName)
    : IRequest, ITransactionalRequest;

