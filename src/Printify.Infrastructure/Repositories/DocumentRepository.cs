namespace Printify.Infrastructure.Repositories;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Printify.Application.Interfaces;
using Printify.Domain.Documents;
using Printify.Infrastructure.Mapping;
using Printify.Infrastructure.Persistence;
using Printify.Infrastructure.Persistence.Entities.Documents;

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
            .FirstOrDefaultAsync(document => document.Id == id, ct)
            .ConfigureAwait(false);

        return entity?.ToDomain();
    }

    public async Task<IReadOnlyList<Document>> ListByPrinterIdAsync(
        Guid printerId,
        DateTimeOffset? beforeCreatedAt,
        Guid? beforeId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int limit,
        CancellationToken ct)
    {
        var effectiveLimit = NormalizeLimit(limit);

        // Always scope the query to the selected printer to avoid leaking other tenants' data.
        var query = dbContext.Documents
            .AsNoTracking()
            .Include(document => document.Elements)
            .Where(document => document.PrinterId == printerId);

        if (from.HasValue)
        {
            query = query.Where(document => document.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(document => document.CreatedAt <= to.Value);
        }

        if (beforeCreatedAt.HasValue)
        {
            var cutoff = beforeCreatedAt.Value;
            // Cursor pagination: exclude newer rows and use Id as a tiebreaker for same timestamp.
            query = query.Where(document =>
                document.CreatedAt < cutoff ||
                (beforeId.HasValue && document.CreatedAt == cutoff && document.Id.CompareTo(beforeId.Value) < 0));
        }

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

    private static int NormalizeLimit(int limit)
    {
        if (limit <= 0)
        {
            return DefaultLimit;
        }

        return Math.Min(limit, MaxLimit);
    }
}
