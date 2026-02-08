using Printify.Infrastructure.Persistence.Entities.Documents;
using Printify.Domain.Documents;
using Printify.Domain.Printers;
using Printify.Domain.Printing;
using EscPosCommands = Printify.Infrastructure.Printing.EscPos.Commands;
using EplCommands = Printify.Infrastructure.Printing.Epl.Commands;
using EplCommandMapper = Printify.Infrastructure.Mapping.Protocols.Epl.CommandMapper;
using EscPosCommandMapper = Printify.Infrastructure.Mapping.Protocols.EscPos.CommandMapper;

namespace Printify.Infrastructure.Mapping;

/// <summary>
/// Bidirectional mapper between Document domain and persistence entities.
/// </summary>
internal static class DocumentMapper
{
    internal static DocumentEntity ToEntity(this Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var entity = new DocumentEntity
        {
            Id = document.Id,
            PrintJobId = document.PrintJobId,
            PrinterId = document.PrinterId,
            Version = 1,
            CreatedAt = document.Timestamp,
            Protocol = EnumMapper.ToString(document.Protocol),
            ClientAddress = document.ClientAddress,
            BytesReceived = document.BytesReceived,
            BytesSent = document.BytesSent,
            WidthInDots = document.WidthInDots,
            HeightInDots = document.HeightInDots
        };

        var elementEntities = new List<DocumentElementEntity>();
        var index = 0;
        foreach (var element in document.Commands)
        {
            var elementEntity = document.Protocol switch
            {
                Protocol.EscPos => ToElementEntity(
                    document.Id,
                    element,
                    EscPosCommandMapper.ToCommandPayload,
                    EscPosCommandMapper.ToEntity,
                    ref index),
                Protocol.Epl => ToElementEntity(
                    document.Id,
                    element,
                    EplCommandMapper.ToCommandPayload,
                    EplCommandMapper.ToEntity,
                    ref index),
                _ => throw new NotSupportedException($"Protocol '{document.Protocol}' is not supported.")
            };

            // Only set MediaId to reference existing media, don't attach media entity
            // This prevents EF from trying to insert duplicate media records
            if (element is EscPosCommands.EscPosRasterImage raster)
            {
                elementEntity.MediaId = raster.Media.Id;
            }
            else if (element is EscPosCommands.EscPosPrintBarcode barcode)
            {
                elementEntity.MediaId = barcode.Media.Id;
            }
            else if (element is EscPosCommands.EscPosPrintQrCode qr)
            {
                elementEntity.MediaId = qr.Media.Id;
            }
            else if (element is EplCommands.EplRasterImage eplRaster)
            {
                elementEntity.MediaId = eplRaster.Media.Id;
            }
            else if (element is EplCommands.EplPrintBarcode eplBarcode)
            {
                elementEntity.MediaId = eplBarcode.Media.Id;
            }

            elementEntities.Add(elementEntity);
        }

        entity.Elements = elementEntities;

        return entity;
    }

    internal static Document ToDomain(this DocumentEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var protocol = EnumMapper.ParseProtocol(entity.Protocol);
        var elements = entity.Elements
            .OrderBy(element => element.Sequence)
            .Select(elementEntity =>
            {
                Command? element = protocol switch
                {
                    Protocol.EscPos => ToDomainElement(
                        elementEntity, EscPosCommandMapper.ToDto, EscPosCommandMapper.ToDomain),
                    Protocol.Epl => ToDomainElement(
                        elementEntity, EplCommandMapper.ToDto, EplCommandMapper.ToDomain),
                    _ => throw new NotSupportedException($"Protocol '{protocol}' is not supported.")
                };

                if (element is null)
                {
                    return null;
                }

                // Legacy rows may not have command raw or length populated yet.
                return element with
                {
                    RawBytes = ParseCommandRaw(elementEntity.CommandRaw),
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
            entity.CreatedAt,
            protocol,
            entity.ClientAddress,
            entity.BytesReceived,
            entity.BytesSent,
            entity.WidthInDots,
            entity.HeightInDots,
            elements,
            null);
    }

    private static DocumentElementEntity ToElementEntity<TPayload>(
        Guid documentId,
        Command element,
        Func<Command, TPayload> toPayload,
        Func<Guid, TPayload, int, byte[], int, DocumentElementEntity> toEntity,
        ref int index)
        where TPayload : notnull
    {
        var payload = toPayload(element);
        var entity = toEntity(
            documentId,
            payload,
            index++,
            element.RawBytes,
            element.LengthInBytes);
        return entity;
    }

    private static Command? ToDomainElement<TPayload>(
        DocumentElementEntity elementEntity,
        Func<DocumentElementEntity, TPayload?> toDto,
        Func<TPayload, Domain.Media.Media?, Command> toDomain)
        where TPayload : notnull
    {
        var dto = toDto(elementEntity);
        if (dto is null)
        {
            return null;
        }

        var media = elementEntity.Media?.ToDomain();

        return toDomain(dto, media);
    }

    internal static byte[] ParseCommandRaw(string? commandRaw)
    {
        if (string.IsNullOrWhiteSpace(commandRaw))
        {
            return [];
        }

        try
        {
            return Convert.FromHexString(commandRaw);
        }
        catch (FormatException)
        {
            // Fallback for legacy non-hex data stored as plain text.
            return System.Text.Encoding.UTF8.GetBytes(commandRaw);
        }
    }
}
