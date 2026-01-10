using Mediator.Net.Contracts;
using Printify.Application.Interfaces;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Workspaces.DeleteWorkspace;

public sealed record DeleteWorkspaceCommand(
    RequestContext Context)
    : IRequest, ITransactionalRequest;
