using Mediator.Net.Contracts;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Workspaces.GetWorkspaceSummary;

public sealed record GetWorkspaceSummaryQuery(RequestContext Context) : IRequest;

