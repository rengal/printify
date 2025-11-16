namespace Printify.Infrastructure.Repositories;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Printify.Application.Interfaces;
using Printify.Domain.Documents;
using Printify.Infrastructure.Documents;
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

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

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

        return entity is null ? null : MapToDomain(entity);
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
            .Select(MapToDomain)
            .ToList();

        return new ReadOnlyCollection<Document>(documents);
    }

    public async Task AddAsync(Document document, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);

        var entity = CreateEntity(document);
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

    private static Document CreateDomainDocument(
        DocumentEntity entity,
        IReadOnlyCollection<DocumentElementDto> elementDtos)
    {
        var protocol = ProtocolMapper.ParseProtocol(entity.Protocol);
        var elements = elementDtos
            .Select(DocumentElementMapper.ToDomain)
            .ToArray();

        return new Document(
            entity.Id,
            entity.PrintJobId,
            entity.PrinterId,
            entity.Version == 0 ? Document.CurrentVersion : entity.Version, // Backfill version for legacy payloads.
            entity.CreatedAt,
            protocol,
            entity.ClientAddress,
            elements);
    }

    private static Document MapToDomain(DocumentEntity entity)
    {
        // Elements are stored as individual rows so we must restore the original order explicitly.
        var orderedElements = entity.Elements
            .OrderBy(element => element.Sequence)
            .Select(DeserializeElement)
            .Where(dto => dto is not null)
            .Select(dto => dto!)
            .ToArray();

        return CreateDomainDocument(entity, orderedElements);
    }

    private static DocumentEntity CreateEntity(Document document)
    {
        var entity = new DocumentEntity
        {
            Id = document.Id,
            PrintJobId = document.PrintJobId,
            PrinterId = document.PrinterId,
            Version = document.Version == 0 ? Document.CurrentVersion : document.Version,
            CreatedAt = document.CreatedAt,
            Protocol = ProtocolMapper.ToString(document.Protocol),
            ClientAddress = document.ClientAddress
        };

        entity.Elements = document.Elements
            .Select(DocumentElementMapper.ToDto)
            .Select((dto, index) => CreateElementEntity(dto, document.Id, index))
            .ToList();

        return entity;
    }

    private static DocumentElementEntity CreateElementEntity(DocumentElementDto dto, Guid documentId, int sequence)
    {
        // Persist both the raw payload (for backwards compatibility) and the resolved type discriminator.
        var payload = JsonSerializer.Serialize(dto, SerializerOptions);

        return new DocumentElementEntity
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Sequence = sequence,
            ElementType = ResolveElementType(dto),
            Payload = payload
        };
    }

    private static DocumentElementDto? DeserializeElement(DocumentElementEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Payload))
        {
            return null;
        }

        return JsonSerializer.Deserialize<DocumentElementDto>(entity.Payload, SerializerOptions);
    }

    private static string ResolveElementType(DocumentElementDto dto)
    {
        return dto switch
        {
            BellElementDto => DocumentElementTypeNames.Bell,
            ErrorElementDto => DocumentElementTypeNames.Error,
            PagecutElementDto => DocumentElementTypeNames.Pagecut,
            PrinterErrorElementDto => DocumentElementTypeNames.PrinterError,
            PrinterStatusElementDto => DocumentElementTypeNames.PrinterStatus,
            PrintBarcodeElementDto => DocumentElementTypeNames.PrintBarcode,
            PrintQrCodeElementDto => DocumentElementTypeNames.PrintQrCode,
            PulseElementDto => DocumentElementTypeNames.Pulse,
            ResetPrinterElementDto => DocumentElementTypeNames.ResetPrinter,
            SetBarcodeHeightElementDto => DocumentElementTypeNames.SetBarcodeHeight,
            SetBarcodeLabelPositionElementDto => DocumentElementTypeNames.SetBarcodeLabelPosition,
            SetBarcodeModuleWidthElementDto => DocumentElementTypeNames.SetBarcodeModuleWidth,
            SetBoldModeElementDto => DocumentElementTypeNames.SetBoldMode,
            SetCodePageElementDto => DocumentElementTypeNames.SetCodePage,
            SetFontElementDto => DocumentElementTypeNames.SetFont,
            SetJustificationElementDto => DocumentElementTypeNames.SetJustification,
            SetLineSpacingElementDto => DocumentElementTypeNames.SetLineSpacing,
            ResetLineSpacingElementDto => DocumentElementTypeNames.ResetLineSpacing,
            SetQrErrorCorrectionElementDto => DocumentElementTypeNames.SetQrErrorCorrection,
            SetQrModelElementDto => DocumentElementTypeNames.SetQrModel,
            SetQrModuleSizeElementDto => DocumentElementTypeNames.SetQrModuleSize,
            SetReverseModeElementDto => DocumentElementTypeNames.SetReverseMode,
            SetUnderlineModeElementDto => DocumentElementTypeNames.SetUnderlineMode,
            StoreQrDataElementDto => DocumentElementTypeNames.StoreQrData,
            StoredLogoElementDto => DocumentElementTypeNames.StoredLogo,
            TextLineElementDto => DocumentElementTypeNames.TextLine,
            _ => throw new NotSupportedException($"Element DTO '{dto.GetType().Name}' is not supported.")
        };
    }
}
