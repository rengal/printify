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
                elementEntity.Media = DocumentMediaEntityMapper.ToEntity(raster.Media);
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

    internal static DocumentElementEntity ToEntity(Guid documentId, DocumentElementPayload dto, int sequence)
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
            TextLineElementPayload => DocumentElementTypeNames.TextLine,
            RasterImageElementPayload => DocumentElementTypeNames.RasterImage,
            _ => throw new NotSupportedException($"Element DTO '{dto.GetType().Name}' is not supported.")
        };
    }
}

internal static class DocumentMediaEntityMapper
{
    internal static DocumentMediaEntity ToEntity(Media media)
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
            Url = media.Url
        };
    }

    internal static Media ToDomain(DocumentMediaEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new Media(
            entity.Id,
            entity.OwnerWorkspaceId,
            entity.CreatedAt,
            entity.IsDeleted,
            entity.ContentType,
            entity.Length,
            entity.Checksum,
            entity.Url);
    }
}

