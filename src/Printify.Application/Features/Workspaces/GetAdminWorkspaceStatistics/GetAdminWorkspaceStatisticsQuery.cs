using Mediator.Net.Contracts;
using Printify.Domain.Requests;

namespace Printify.Application.Features.Workspaces.GetAdminWorkspaceStatistics;

public sealed record GetAdminWorkspaceStatisticsQuery(
    RequestContext Context) : IRequest;
