using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Printify.Domain.Config;
using Printify.Infrastructure.Persistence;
using Printify.Infrastructure.Persistence.Entities.Documents;

namespace Printify.Infrastructure.Retention;

public sealed class DocumentRetentionCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<DocumentCleanupOptions> cleanupOptions,
    IOptions<Storage> storageOptions,
    ILogger<DocumentRetentionCleanupService> logger)
{
    private const long DayInMs = 24L * 60L * 60L * 1000L;
    private const int DefaultBatchSize = 500;
    private const int DefaultMaxBatchesPerRun = 20;

    private readonly string mediaRootPath = ResolveMediaRootPath(storageOptions.Value.MediaRootPath);

    public async Task<DocumentRetentionCleanupResult> RunOnceAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var totalDocuments = 0;
        var totalMedia = 0;
        var maxBatches = NormalizePositive(cleanupOptions.Value.MaxBatchesPerRun, DefaultMaxBatchesPerRun);

        for (var batch = 0; batch < maxBatches; batch++)
        {
            var result = await DeleteExpiredBatchAsync(now, cancellationToken).ConfigureAwait(false);
            totalDocuments += result.DeletedDocuments;
            totalMedia += result.DeletedMedia;

            if (result.DeletedDocuments == 0)
            {
                break;
            }
        }

        if (totalDocuments > 0 || totalMedia > 0)
        {
            logger.LogInformation(
                "Document retention cleanup deleted {DocumentCount} documents and {MediaCount} media files",
                totalDocuments,
                totalMedia);
        }

        return new DocumentRetentionCleanupResult(totalDocuments, totalMedia);
    }

    private async Task<DocumentRetentionCleanupResult> DeleteExpiredBatchAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrintifyDbContext>();
        var batchSize = NormalizePositive(cleanupOptions.Value.BatchSize, DefaultBatchSize);
        var nowUnixMs = now.ToUnixTimeMilliseconds();

        // Retention is owned by the workspace; zero means documents are kept forever.
        var expiredDocuments = await (
                from document in dbContext.Documents
                join printer in dbContext.Printers on document.PrinterId equals printer.Id
                join workspace in dbContext.Workspaces on printer.OwnerWorkspaceId equals workspace.Id
                where !workspace.IsDeleted
                    && workspace.DocumentRetentionDays > 0
                    && document.CreatedAtUnixMs < nowUnixMs - (workspace.DocumentRetentionDays * DayInMs)
                orderby document.CreatedAtUnixMs
                select new ExpiredDocument(document.Id, document.PrinterId))
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (expiredDocuments.Count == 0)
        {
            return new DocumentRetentionCleanupResult(0, 0);
        }

        var expiredDocumentIds = expiredDocuments
            .Select(document => document.Id)
            .ToArray();
        var affectedPrinterIds = expiredDocuments
            .Select(document => document.PrinterId)
            .Distinct()
            .ToArray();

        List<MediaFile> mediaFilesToDelete;
        int deletedDocuments;

        await using (var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false))
        {
            // Collect candidate media before deleting documents; orphan media is rechecked after cascades.
            var candidateMediaIds = await dbContext.Set<DocumentElementEntity>()
                .Where(element => expiredDocumentIds.Contains(element.DocumentId) && element.MediaId.HasValue)
                .Select(element => element.MediaId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            deletedDocuments = await dbContext.Documents
                .Where(document => expiredDocumentIds.Contains(document.Id))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            await RefreshLastDocumentReceivedAtAsync(
                    dbContext,
                    affectedPrinterIds,
                    cancellationToken)
                .ConfigureAwait(false);

            mediaFilesToDelete = candidateMediaIds.Count == 0
                ? []
                : await dbContext.DocumentMedia
                    .Where(media => candidateMediaIds.Contains(media.Id))
                    .Where(media => !dbContext.Set<DocumentElementEntity>()
                        .Any(element => element.MediaId == media.Id))
                    .Select(media => new MediaFile(media.Id, media.FileName))
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

            if (mediaFilesToDelete.Count > 0)
            {
                var orphanMediaIds = mediaFilesToDelete
                    .Select(media => media.Id)
                    .ToArray();

                await dbContext.DocumentMedia
                    .Where(media => orphanMediaIds.Contains(media.Id))
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (var mediaFile in mediaFilesToDelete)
        {
            DeleteMediaFile(mediaFile.FileName);
        }

        return new DocumentRetentionCleanupResult(deletedDocuments, mediaFilesToDelete.Count);
    }

    private void DeleteMediaFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(mediaRootPath) || string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        var fullPath = Path.GetFullPath(Path.Combine(mediaRootPath, fileName));
        if (!IsUnderRoot(fullPath, mediaRootPath))
        {
            logger.LogWarning("Skipping media cleanup for path outside storage root: {FileName}", fileName);
            return;
        }

        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "Failed to delete expired media file {FileName}", fileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Failed to delete expired media file {FileName}", fileName);
        }
    }

    private static string ResolveMediaRootPath(string? configuredPath)
    {
        return string.IsNullOrWhiteSpace(configuredPath)
            ? string.Empty
            : Path.GetFullPath(configuredPath);
    }

    private static bool IsUnderRoot(string fullPath, string rootPath)
    {
        var normalizedRoot = rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? rootPath
            : rootPath + Path.DirectorySeparatorChar;

        return fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static int NormalizePositive(int value, int fallback)
    {
        return value > 0 ? value : fallback;
    }

    private static async Task RefreshLastDocumentReceivedAtAsync(
        PrintifyDbContext dbContext,
        Guid[] printerIds,
        CancellationToken cancellationToken)
    {
        if (printerIds.Length == 0)
        {
            return;
        }

        // Last-document metadata is denormalized for fast sidebar sorting, so cleanup must keep it consistent.
        var latestDocumentByPrinterId = await dbContext.Documents
            .Where(document => printerIds.Contains(document.PrinterId))
            .GroupBy(document => document.PrinterId)
            .Select(group => new
            {
                PrinterId = group.Key,
                LastDocumentUnixMs = group.Max(document => document.CreatedAtUnixMs)
            })
            .ToDictionaryAsync(
                item => item.PrinterId,
                item => item.LastDocumentUnixMs,
                cancellationToken)
            .ConfigureAwait(false);

        var printers = await dbContext.Printers
            .Where(printer => printerIds.Contains(printer.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var printer in printers)
        {
            printer.LastDocumentReceivedAt = latestDocumentByPrinterId.TryGetValue(printer.Id, out var lastUnixMs)
                ? DateTimeOffset.FromUnixTimeMilliseconds(lastUnixMs)
                : null;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private sealed record ExpiredDocument(Guid Id, Guid PrinterId);

    private sealed record MediaFile(Guid Id, string FileName);
}
