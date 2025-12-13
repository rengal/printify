namespace Printify.Infrastructure.Repositories;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Printify.Application.Interfaces;
using Printify.Domain.Documents;
using Printify.Domain.Media;
using Printify.Infrastructure.Mapping;
using Printify.Infrastructure.Persistence;

/// <summary>
/// Persists printer documents inside the shared DbContext so they can be queried and streamed later.
/// </summary>
public sealed class DocumentRepository : IDocumentRepository
{
    private const int DefaultLimit = 20;
    private const int MaxLimit = 200;

    private readonly PrintifyDbContext dbContext;

    public DocumentRepository(PrintifyDbContext dbContext)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async ValueTask<Document?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await dbContext.Documents
            .AsNoTracking()
            .Include(document => document.Elements)
            .ThenInclude(element => element.Media)
            .FirstOrDefaultAsync(document => document.Id == id, ct)
            .ConfigureAwait(false);

        return entity?.ToDomain();
    }

    public async Task<IReadOnlyList<Document>> ListByPrinterIdAsync(
        Guid printerId,
        Guid? beforeId,
        int limit,
        CancellationToken ct)
    {
        var effectiveLimit = NormalizeLimit(limit);

        // Always scope the query to the selected printer to avoid leaking other tenants' data.
        var query = dbContext.Documents
            .AsNoTracking()
            .Include(document => document.Elements)
            .ThenInclude(element => element.Media)
            .Where(document => document.PrinterId == printerId);

        var entities = await query
            .OrderByDescending(document => document.CreatedAt)
            .ThenByDescending(document => document.Id)
            .Take(effectiveLimit)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var documents = entities
            .Select(DocumentEntityMapper.ToDomain)
            .ToList();

        return new ReadOnlyCollection<Document>(documents);
    }

    public async Task AddAsync(Document document, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);

        var entity = document.ToEntity();
        await dbContext.Documents.AddAsync(entity, ct).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public Task<long> CountByPrinterIdAsync(Guid printerId, CancellationToken ct)
    {
        return dbContext.Documents
            .AsNoTracking()
            .Where(document => document.PrinterId == printerId)
            .LongCountAsync(ct);
    }

    public async ValueTask<Media?> GetMediaByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await dbContext.DocumentMedia
            .AsNoTracking()
            .FirstOrDefaultAsync(media => media.Id == id, ct)
            .ConfigureAwait(false);

        return entity is null ? null : DocumentMediaEntityMapper.ToDomain(entity);
    }

    public async ValueTask<Media?> GetMediaByChecksumAsync(string checksum, Guid? ownerWorkspaceId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(checksum))
            return null;

        var entity = await dbContext.DocumentMedia
            .AsNoTracking()
            .Where(media => media.Checksum == checksum && (ownerWorkspaceId == null || media.OwnerWorkspaceId == ownerWorkspaceId))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return entity is null ? null : DocumentMediaEntityMapper.ToDomain(entity);
    }

    private static int NormalizeLimit(int limit)
    {
        if (limit <= 0)
        {
            return DefaultLimit;
        }

        return Math.Min(limit, MaxLimit);
    }
}
