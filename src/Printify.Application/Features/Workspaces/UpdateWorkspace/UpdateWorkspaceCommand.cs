using Mediator.Net.Contracts;
using Printify.Application.Interfaces;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Workspaces.UpdateWorkspace;

public sealed record UpdateWorkspaceCommand(
    RequestContext Context,
    string? Name,
    int? DocumentRetentionDays)
    : IRequest, ITransactionalRequest;
