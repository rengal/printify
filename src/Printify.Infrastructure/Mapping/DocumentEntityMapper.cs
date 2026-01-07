using System.Text.Json;
using System.Text.Json.Serialization;
using Printify.Domain.Mapping;
using Printify.Domain.Documents;
using Printify.Domain.Documents.Elements;
using Printify.Infrastructure.Documents;
using Printify.Infrastructure.Persistence.Entities.Documents;

namespace Printify.Infrastructure.Mapping;

internal static class DocumentEntityMapper
{
    internal static DocumentEntity ToEntity(this Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var entity = new DocumentEntity
        {
            Id = document.Id,
            PrintJobId = document.PrintJobId,
            PrinterId = document.PrinterId,
            Version = document.Version == 0 ? Document.CurrentVersion : document.Version,
            CreatedAt = document.CreatedAt,
            Protocol = DomainMapper.ToString(document.Protocol),
            WidthInDots = document.WidthInDots,
            HeightInDots = document.HeightInDots,
            ClientAddress = document.ClientAddress,
            BytesReceived = document.BytesReceived,
            BytesSent = document.BytesSent
        };

        var elementEntities = new List<DocumentElementEntity>();
        var elements = document.Elements;
        var index = 0;
        foreach (var element in elements)
        {
            var dto = DocumentElementMapper.ToDto(element);
            var elementEntity = DocumentElementEntityMapper.ToEntity(document.Id, dto, index++, element.CommandRaw, element.LengthInBytes);

            // Only set MediaId to reference existing media, don't attach media entity
            // This prevents EF from trying to insert duplicate media records
            if (element is RasterImage raster)
            {
                elementEntity.MediaId = raster.Media.Id;
            }
            else if (element is PrintBarcode barcode)
            {
                elementEntity.MediaId = barcode.Media.Id;
            }
            else if (element is PrintQrCode qr)
            {
                elementEntity.MediaId = qr.Media.Id;
            }

            elementEntities.Add(elementEntity);
        }

        entity.Elements = elementEntities;

        return entity;
    }

    internal static Document ToDomain(this DocumentEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var protocol = DomainMapper.ParseProtocol(entity.Protocol);
        var elements = entity.Elements
            .OrderBy(element => element.Sequence)
            .Select(elementEntity =>
            {
                var dto = DocumentElementEntityMapper.ToDto(elementEntity);
                if (dto is null)
                {
                    return null;
                }

                Domain.Media.Media? media = elementEntity.Media is null
                    ? null
                    : DocumentMediaEntityMapper.ToDomain(elementEntity.Media);

                var element = DocumentElementMapper.ToDomain(dto, media);
                // Legacy rows may not have command raw or length populated yet.
                return element with
                {
                    CommandRaw = elementEntity.CommandRaw ?? string.Empty,
                    LengthInBytes = elementEntity.LengthInBytes
                };
            })
            .Where(element => element is not null)
            .Select(element => element!)
            .ToArray();

        return new Document(
            entity.Id,
            entity.PrintJobId,
            entity.PrinterId,
            entity.Version == 0 ? Document.CurrentVersion : entity.Version,
            entity.CreatedAt,
            protocol,
            entity.WidthInDots,
            entity.HeightInDots,
            entity.ClientAddress,
            entity.BytesReceived,
            entity.BytesSent,
            elements);
    }
}

internal static class DocumentElementEntityMapper
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    internal static DocumentElementEntity ToEntity(
        Guid documentId,
        DocumentElementPayload dto,
        int sequence,
        string commandRaw,
        int lengthInBytes)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(commandRaw);

        return new DocumentElementEntity
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Sequence = sequence,
            ElementType = ResolveElementType(dto),
            Payload = JsonSerializer.Serialize(dto, SerializerOptions),
            CommandRaw = commandRaw,
            LengthInBytes = lengthInBytes
        };
    }

    internal static DocumentElementPayload? ToDto(DocumentElementEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (string.IsNullOrWhiteSpace(entity.Payload))
        {
            return null;
        }

        return JsonSerializer.Deserialize<DocumentElementPayload>(entity.Payload, SerializerOptions);
    }

    private static string ResolveElementType(DocumentElementPayload dto)
    {
        return dto switch
        {
            BellElementPayload => DocumentElementTypeNames.Bell,
            ErrorElementPayload => DocumentElementTypeNames.Error,
            PagecutElementPayload => DocumentElementTypeNames.Pagecut,
            PrinterErrorElementPayload => DocumentElementTypeNames.PrinterError,
            PrinterStatusElementPayload => DocumentElementTypeNames.PrinterStatus,
            PrintBarcodeElementPayload => DocumentElementTypeNames.PrintBarcode,
            PrintQrCodeElementPayload => DocumentElementTypeNames.PrintQrCode,
            PulseElementPayload => DocumentElementTypeNames.Pulse,
            ResetPrinterElementPayload => DocumentElementTypeNames.ResetPrinter,
            SetBarcodeHeightElementPayload => DocumentElementTypeNames.SetBarcodeHeight,
            SetBarcodeLabelPositionElementPayload => DocumentElementTypeNames.SetBarcodeLabelPosition,
            SetBarcodeModuleWidthElementPayload => DocumentElementTypeNames.SetBarcodeModuleWidth,
            SetBoldModeElementPayload => DocumentElementTypeNames.SetBoldMode,
            SetCodePageElementPayload => DocumentElementTypeNames.SetCodePage,
            SetFontElementPayload => DocumentElementTypeNames.SetFont,
            SetJustificationElementPayload => DocumentElementTypeNames.SetJustification,
            SetLineSpacingElementPayload => DocumentElementTypeNames.SetLineSpacing,
            ResetLineSpacingElementPayload => DocumentElementTypeNames.ResetLineSpacing,
            SetQrErrorCorrectionElementPayload => DocumentElementTypeNames.SetQrErrorCorrection,
            SetQrModelElementPayload => DocumentElementTypeNames.SetQrModel,
            SetQrModuleSizeElementPayload => DocumentElementTypeNames.SetQrModuleSize,
            SetReverseModeElementPayload => DocumentElementTypeNames.SetReverseMode,
            SetUnderlineModeElementPayload => DocumentElementTypeNames.SetUnderlineMode,
            StoreQrDataElementPayload => DocumentElementTypeNames.StoreQrData,
            StoredLogoElementPayload => DocumentElementTypeNames.StoredLogo,
            AppendToLineBufferElementPayload => DocumentElementTypeNames.AppendToLineBuffer,
            FlushLineBufferAndFeedElementPayload => DocumentElementTypeNames.FlushLineBufferAndFeed,
            LegacyCarriageReturnElementPayload => DocumentElementTypeNames.LegacyCarriageReturn,
            RasterImageElementPayload => DocumentElementTypeNames.RasterImage,
            StatusRequestElementPayload => DocumentElementTypeNames.StatusRequest,
            StatusResponseElementPayload => DocumentElementTypeNames.StatusResponse,
            _ => throw new NotSupportedException($"Element DTO '{dto.GetType().Name}' is not supported.")
        };
    }
}

internal static class DocumentMediaEntityMapper
{
    internal static DocumentMediaEntity ToEntity(Domain.Media.Media media)
    {
        ArgumentNullException.ThrowIfNull(media);

        return new DocumentMediaEntity
        {
            Id = media.Id,
            OwnerWorkspaceId = media.OwnerWorkspaceId,
            CreatedAt = media.CreatedAt,
            IsDeleted = media.IsDeleted,
            ContentType = media.ContentType,
            Length = media.Length,
            Checksum = media.Sha256Checksum,
            FileName = media.FileName,
            Url = media.Url
        };
    }

    internal static Domain.Media.Media ToDomain(DocumentMediaEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new Domain.Media.Media(
            entity.Id,
            entity.OwnerWorkspaceId,
            entity.CreatedAt,
            entity.IsDeleted,
            entity.ContentType,
            entity.Length,
            entity.Checksum,
            entity.FileName,
            entity.Url);
    }
}
