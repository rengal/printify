using Printify.Application.Features.Workspaces.GetAdminWorkspaceStatistics;

namespace Printify.Application.Interfaces;

public interface IAdminWorkspaceStatisticsRepository
{
    Task<AdminWorkspaceStatistics> GetAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
