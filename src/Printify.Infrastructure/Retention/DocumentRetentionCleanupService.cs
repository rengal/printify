using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Printify.Application.Interfaces;
using Printify.Domain.Config;
using Printify.Infrastructure.Persistence;
using Printify.Infrastructure.Persistence.Entities.Documents;

namespace Printify.Infrastructure.Retention;

public sealed class DocumentRetentionCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<DocumentCleanupOptions> cleanupOptions,
    IOptions<Storage> storageOptions,
    ILogger<DocumentRetentionCleanupService> logger)
    : IPrinterDocumentCleaner
{
    private const long DayInMs = 24L * 60L * 60L * 1000L;
    private const int DefaultBatchSize = 500;
    private const int DefaultMaxBatchesPerRun = 20;

    private readonly string mediaRootPath = ResolveMediaRootPath(storageOptions.Value.MediaRootPath);

    public async Task<DocumentRetentionCleanupResult> RunOnceAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return await RunOnceAsync(now, workspaceId: null, maxDocuments: null, retentionDaysOverride: null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DocumentRetentionCleanupResult> RunOnceAsync(
        DateTimeOffset now,
        Guid? workspaceId,
        int? maxDocuments,
        int? retentionDaysOverride,
        CancellationToken cancellationToken)
    {
        var totalDocuments = 0;
        var totalMedia = 0;
        var maxBatches = NormalizePositive(cleanupOptions.Value.MaxBatchesPerRun, DefaultMaxBatchesPerRun);
        var remainingDocuments = maxDocuments;

        for (var batch = 0; batch < maxBatches && remainingDocuments != 0; batch++)
        {
            var result = await DeleteExpiredBatchAsync(now, workspaceId, remainingDocuments, retentionDaysOverride, cancellationToken)
                .ConfigureAwait(false);
            totalDocuments += result.DeletedDocuments;
            totalMedia += result.DeletedMedia;
            remainingDocuments -= result.DeletedDocuments;

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

    public async Task<DocumentRetentionCleanupSummary> GetSummaryAsync(
        DateTimeOffset now,
        Guid? workspaceId,
        int? retentionDaysOverride,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrintifyDbContext>();
        var expiredDocumentIds = CreateExpiredDocumentIdQuery(dbContext, now, workspaceId, retentionDaysOverride);

        var expiredDocuments = await expiredDocumentIds.CountAsync(cancellationToken).ConfigureAwait(false);
        if (expiredDocuments == 0)
        {
            return new DocumentRetentionCleanupSummary(0, 0);
        }

        var retentionMediaFiles = await CountRetentionMediaFilesAsync(
                dbContext,
                expiredDocumentIds,
                cancellationToken)
            .ConfigureAwait(false);

        return new DocumentRetentionCleanupSummary(expiredDocuments, retentionMediaFiles);
    }

    private async Task<DocumentRetentionCleanupResult> DeleteExpiredBatchAsync(
        DateTimeOffset now,
        Guid? workspaceId,
        int? maxDocuments,
        int? retentionDaysOverride,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrintifyDbContext>();
        var configuredBatchSize = NormalizePositive(cleanupOptions.Value.BatchSize, DefaultBatchSize);
        var batchSize = maxDocuments.HasValue
            ? Math.Min(configuredBatchSize, maxDocuments.Value)
            : configuredBatchSize;

        var expiredDocuments = await CreateExpiredDocumentQuery(dbContext, now, workspaceId, retentionDaysOverride)
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

        return await DeleteDocumentsAndMediaAsync(
                dbContext,
                expiredDocumentIds,
                affectedPrinterIds,
                cancellationToken)
            .ConfigureAwait(false);
    }

    async Task IPrinterDocumentCleaner.DeleteByPrinterAsync(Guid printerId, CancellationToken cancellationToken)
    {
        await DeleteByPrinterAsync(printerId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes all documents (and their now-orphaned media files) belonging to a single printer.
    /// Used when clearing a printer's history or deleting the printer, so no orphan rows or files remain.
    /// </summary>
    public async Task<DocumentRetentionCleanupResult> DeleteByPrinterAsync(
        Guid printerId,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrintifyDbContext>();

        var documentIds = await dbContext.Documents
            .Where(document => document.PrinterId == printerId)
            .Select(document => document.Id)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        if (documentIds.Length == 0)
        {
            return new DocumentRetentionCleanupResult(0, 0);
        }

        return await DeleteDocumentsAndMediaAsync(
                dbContext,
                documentIds,
                affectedPrinterIds: [printerId],
                cancellationToken)
            .ConfigureAwait(false);
    }

    // Deletes the given documents in a transaction, removes media no longer referenced by any element,
    // refreshes denormalized printer metadata, then deletes the freed media files from disk.
    private async Task<DocumentRetentionCleanupResult> DeleteDocumentsAndMediaAsync(
        PrintifyDbContext dbContext,
        Guid[] documentIds,
        Guid[] affectedPrinterIds,
        CancellationToken cancellationToken)
    {
        List<MediaFile> mediaFilesToDelete;
        int deletedDocuments;

        await using (var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false))
        {
            // Collect candidate media before deleting documents; orphan media is rechecked after cascades.
            var candidateMediaIds = await dbContext.Set<DocumentElementEntity>()
                .Where(element => documentIds.Contains(element.DocumentId) && element.MediaId.HasValue)
                .Select(element => element.MediaId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            deletedDocuments = await dbContext.Documents
                .Where(document => documentIds.Contains(document.Id))
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
                    // A media file is removable only after no remaining document element references it.
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

    private static IQueryable<ExpiredDocument> CreateExpiredDocumentQuery(
        PrintifyDbContext dbContext,
        DateTimeOffset now,
        Guid? workspaceId,
        int? retentionDaysOverride)
    {
        return CreateExpiredDocumentBaseQuery(dbContext, now, workspaceId, retentionDaysOverride)
            .OrderBy(document => document.CreatedAtUnixMs)
            .Select(document => new ExpiredDocument(document.Id, document.PrinterId));
    }

    private static IQueryable<Guid> CreateExpiredDocumentIdQuery(
        PrintifyDbContext dbContext,
        DateTimeOffset now,
        Guid? workspaceId,
        int? retentionDaysOverride)
    {
        // This shape stays fully SQL-translatable for Contains subqueries used by media reference checks.
        return CreateExpiredDocumentBaseQuery(dbContext, now, workspaceId, retentionDaysOverride)
            .Select(document => document.Id);
    }

    private static IQueryable<DocumentEntity> CreateExpiredDocumentBaseQuery(
        PrintifyDbContext dbContext,
        DateTimeOffset now,
        Guid? workspaceId,
        int? retentionDaysOverride)
    {
        var nowUnixMs = now.ToUnixTimeMilliseconds();
        var overrideCutoffUnixMs = ResolveOverrideCutoffUnixMs(nowUnixMs, retentionDaysOverride);

        // A document is expired when its owning workspace's retention applies, OR when it is orphaned
        // (no existing printer) — orphans are garbage that the INNER-JOIN form could never reach.
        // An admin override applies a single cutoff to every workspace (including keep-forever ones);
        // without it, retention is owned by the workspace and zero days means keep forever.
        return
            from document in dbContext.Documents
            join printer in dbContext.Printers on document.PrinterId equals printer.Id into printerGroup
            from printer in printerGroup.DefaultIfEmpty()
            join workspace in dbContext.Workspaces on printer.OwnerWorkspaceId equals workspace.Id into workspaceGroup
            from workspace in workspaceGroup.DefaultIfEmpty()
            where
                // Orphaned document: the referenced printer no longer exists.
                printer == null
                // Or a normal document whose workspace is in scope and past its retention cutoff.
                || (!workspace.IsDeleted
                    && (!workspaceId.HasValue || workspace.Id == workspaceId.Value)
                    && (overrideCutoffUnixMs.HasValue
                        ? document.CreatedAtUnixMs < overrideCutoffUnixMs.Value
                        : workspace.DocumentRetentionDays > 0
                            && document.CreatedAtUnixMs < nowUnixMs - (workspace.DocumentRetentionDays * DayInMs)))
            select document;
    }

    // Translates an admin "max retention days" override into an absolute cutoff timestamp.
    // null override => no override (use per-workspace settings); 0 days => delete everything (cutoff == now).
    private static long? ResolveOverrideCutoffUnixMs(long nowUnixMs, int? retentionDaysOverride)
    {
        if (!retentionDaysOverride.HasValue)
        {
            return null;
        }

        var days = Math.Max(0, retentionDaysOverride.Value);
        return nowUnixMs - (days * DayInMs);
    }

    private static async Task<int> CountRetentionMediaFilesAsync(
        PrintifyDbContext dbContext,
        IQueryable<Guid> expiredDocumentIds,
        CancellationToken cancellationToken)
    {
        return await dbContext.DocumentMedia
            // The media file must be referenced by at least one document that retention would delete.
            .Where(media => dbContext.Set<DocumentElementEntity>()
                .Any(element => element.MediaId == media.Id && expiredDocumentIds.Contains(element.DocumentId)))
            // The media file must not be referenced by any document that retention would keep.
            .Where(media => !dbContext.Set<DocumentElementEntity>()
                .Any(element => element.MediaId == media.Id && !expiredDocumentIds.Contains(element.DocumentId)))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
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
