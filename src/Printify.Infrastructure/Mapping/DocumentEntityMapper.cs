namespace Printify.Infrastructure.Mapping;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Printify.Domain.Documents;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Media;
using Printify.Infrastructure.Documents;
using Printify.Infrastructure.Persistence.Entities.Documents;

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
            Protocol = ProtocolMapper.ToString(document.Protocol),
            ClientAddress = document.ClientAddress
        };

        var elementEntities = new List<DocumentElementEntity>();
        var elements = document.Elements ?? Array.Empty<Element>();
        var index = 0;
        foreach (var element in elements)
        {
            var dto = DocumentElementMapper.ToDto(element);
            var elementEntity = DocumentElementEntityMapper.ToEntity(document.Id, dto, index++);
            if (element is RasterImage raster)
            {
                elementEntity.Media = DocumentMediaEntityMapper.ToEntity(elementEntity.Id, raster.Media);
            }

            elementEntities.Add(elementEntity);
        }

        entity.Elements = elementEntities;

        return entity;
    }

    internal static Document ToDomain(this DocumentEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var protocol = ProtocolMapper.ParseProtocol(entity.Protocol);
        var elements = entity.Elements
            .OrderBy(element => element.Sequence)
            .Select(elementEntity =>
            {
                var dto = DocumentElementEntityMapper.ToDto(elementEntity);
                if (dto is null)
                {
                    return null;
                }

                Media? media = elementEntity.Media is null
                    ? null
                    : DocumentMediaEntityMapper.ToDomain(elementEntity.Media);

                return DocumentElementMapper.ToDomain(dto, media);
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
            entity.ClientAddress,
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

    internal static DocumentElementEntity ToEntity(Guid documentId, DocumentElementDto dto, int sequence)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new DocumentElementEntity
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Sequence = sequence,
            ElementType = ResolveElementType(dto),
            Payload = JsonSerializer.Serialize(dto, SerializerOptions)
        };
    }

    internal static DocumentElementDto? ToDto(DocumentElementEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

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

internal static class DocumentMediaEntityMapper
{
    internal static DocumentMediaEntity ToEntity(Guid documentElementId, Media media)
    {
        ArgumentNullException.ThrowIfNull(media);

        return new DocumentMediaEntity
        {
            Id = media.Id,
            DocumentElementId = documentElementId,
            CreatedAt = media.CreatedAt,
            IsDeleted = media.IsDeleted,
            ContentType = media.ContentType,
            Length = media.Length,
            Checksum = media.Checksum,
            Url = media.Url
        };
    }

    internal static Media ToDomain(DocumentMediaEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new Media(
            entity.Id,
            entity.CreatedAt,
            entity.IsDeleted,
            entity.ContentType,
            entity.Length,
            entity.Checksum,
            entity.Url);
    }
}
