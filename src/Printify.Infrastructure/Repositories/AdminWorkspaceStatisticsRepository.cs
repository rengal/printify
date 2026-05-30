using Microsoft.EntityFrameworkCore;
using Printify.Application.Features.Workspaces.GetAdminWorkspaceStatistics;
using Printify.Application.Interfaces;
using Printify.Infrastructure.Persistence;

namespace Printify.Infrastructure.Repositories;

public sealed class AdminWorkspaceStatisticsRepository(PrintifyDbContext dbContext)
    : IAdminWorkspaceStatisticsRepository
{
    public async Task<AdminWorkspaceStatistics> GetAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var last24hUnixMs = now.AddHours(-24).ToUnixTimeMilliseconds();
        var last7dUnixMs = now.AddDays(-7).ToUnixTimeMilliseconds();
        var last24h = now.AddHours(-24);
        var last7d = now.AddDays(-7);

        var workspaces = await dbContext.Workspaces
            .AsNoTracking()
            .Where(workspace => !workspace.IsDeleted)
            .Select(workspace => new
            {
                workspace.Id,
                workspace.Name,
                workspace.Role,
                workspace.DocumentRetentionDays
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var printerCounts = await dbContext.Printers
            .AsNoTracking()
            .GroupBy(printer => printer.OwnerWorkspaceId)
            .Select(group => new
            {
                WorkspaceId = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(row => row.WorkspaceId, row => row.Count, cancellationToken)
            .ConfigureAwait(false);

        var documentStats = await (
                from printer in dbContext.Printers.AsNoTracking()
                join document in dbContext.Documents.AsNoTracking() on printer.Id equals document.PrinterId
                group document by printer.OwnerWorkspaceId
                into groupDocuments
                select new
                {
                    WorkspaceId = groupDocuments.Key,
                    Count = groupDocuments.LongCount(),
                    Last24h = groupDocuments.LongCount(document => document.CreatedAtUnixMs >= last24hUnixMs),
                    Last7d = groupDocuments.LongCount(document => document.CreatedAtUnixMs >= last7dUnixMs),
                    LastDocumentUnixMs = groupDocuments.Max(document => (long?)document.CreatedAtUnixMs)
                })
            .ToDictionaryAsync(row => row.WorkspaceId, row => row, cancellationToken)
            .ConfigureAwait(false);

        var mediaItems = await dbContext.DocumentMedia
            .AsNoTracking()
            .Where(media => !media.IsDeleted)
            .Select(media => new
            {
                media.OwnerWorkspaceId,
                media.Length,
                media.CreatedAt
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var mediaStats = mediaItems
            .Where(media => media.OwnerWorkspaceId.HasValue)
            .GroupBy(media => media.OwnerWorkspaceId!.Value)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Count = group.LongCount(),
                    Bytes = group.Sum(media => media.Length),
                    Last24h = group.LongCount(media => media.CreatedAt >= last24h),
                    Last7d = group.LongCount(media => media.CreatedAt >= last7d)
                });

        var rows = workspaces
            .Select(workspace =>
            {
                documentStats.TryGetValue(workspace.Id, out var documents);
                mediaStats.TryGetValue(workspace.Id, out var media);

                return new AdminWorkspaceStatisticsRow(
                    workspace.Id,
                    workspace.Name,
                    workspace.Role,
                    printerCounts.GetValueOrDefault(workspace.Id),
                    documents?.Count ?? 0,
                    media?.Count ?? 0,
                    media?.Bytes ?? 0,
                    documents?.Last24h ?? 0,
                    documents?.LastDocumentUnixMs is { } lastDocumentUnixMs
                        ? DateTimeOffset.FromUnixTimeMilliseconds(lastDocumentUnixMs)
                        : null,
                    workspace.DocumentRetentionDays);
            })
            .OrderByDescending(row => row.LastDocumentAt)
            .ThenBy(row => row.WorkspaceName)
            .ToList();

        var totalWorkspaces = rows.Count;
        var totalPrinters = rows.Sum(row => row.PrinterCount);
        var totalDocuments = rows.Sum(row => row.DocumentCount);
        var totalMedia = mediaItems.LongCount();
        var totalMediaBytes = mediaItems.Sum(media => media.Length);

        var documentsLast24h = rows.Sum(row => row.DocumentsLast24h);
        var documentsLast7d = documentStats.Values.Sum(row => row.Last7d);
        var mediaLast24h = mediaItems.LongCount(media => media.CreatedAt >= last24h);
        var mediaLast7d = mediaItems.LongCount(media => media.CreatedAt >= last7d);

        var activeWorkspacesLast24h = rows.Count(row => row.DocumentsLast24h > 0);
        var activeWorkspacesLast7d = documentStats.Values.Count(row => row.Last7d > 0);

        var lastDocumentAt = rows
            .Where(row => row.LastDocumentAt.HasValue)
            .Select(row => row.LastDocumentAt)
            .FirstOrDefault();

        return new AdminWorkspaceStatistics(
            totalWorkspaces,
            activeWorkspacesLast24h,
            activeWorkspacesLast7d,
            totalPrinters,
            totalDocuments,
            totalMedia,
            totalMediaBytes,
            documentsLast24h,
            documentsLast7d,
            mediaLast24h,
            mediaLast7d,
            lastDocumentAt,
            rows);
    }
}
