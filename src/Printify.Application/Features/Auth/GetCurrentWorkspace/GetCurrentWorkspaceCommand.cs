using Mediator.Net.Contracts;
using Printify.Domain.Requests;
using Printify.Domain.Workspaces;

namespace Printify.Application.Features.Auth.GetCurrentWorkspace;

public record GetCurrentWorkspaceCommand(
    RequestContext Context)
    : IRequest;

