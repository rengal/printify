using Mediator.Net.Contracts;
using Mediator.Net.Context;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Domain.Workspaces;

namespace Printify.Application.Features.Workspaces.GetAdminWorkspaceStatistics;

public sealed class GetAdminWorkspaceStatisticsHandler(
    IWorkspaceRepository workspaceRepository,
    IAdminWorkspaceStatisticsRepository statisticsRepository)
    : IRequestHandler<GetAdminWorkspaceStatisticsQuery, AdminWorkspaceStatistics>
{
    public async Task<AdminWorkspaceStatistics> Handle(
        IReceiveContext<GetAdminWorkspaceStatisticsQuery> context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Message;
        ArgumentNullException.ThrowIfNull(request);

        var workspaceId = request.Context.WorkspaceId;
        if (!workspaceId.HasValue)
        {
            throw new InvalidOperationException("WorkspaceId is required");
        }

        var workspace = await workspaceRepository.GetByIdAsync(workspaceId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (workspace is null)
        {
            throw new InvalidOperationException("Workspace not found");
        }

        if (workspace.Role != WorkspaceRole.Admin)
        {
            throw new ForbiddenException("Admin workspace role is required.");
        }

        return await statisticsRepository
            .GetAsync(DateTimeOffset.UtcNow, cancellationToken)
            .ConfigureAwait(false);
    }
}
