using Printify.Domain.Mapping;
using Printify.Domain.Printers;
using Printify.Infrastructure.Persistence.Entities.Printers;

namespace Printify.Infrastructure.Mapping;

internal static class PrinterEntityMapper
{
    internal static void MapToEntity(this Printer printer, PrinterEntity entity)
    {
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentNullException.ThrowIfNull(entity);
        entity.Id = printer.Id;
        entity.OwnerWorkspaceId = printer.OwnerWorkspaceId;
        entity.DisplayName = printer.DisplayName;
        entity.Protocol = DomainMapper.ToString(printer.Protocol);
        entity.WidthInDots = printer.WidthInDots;
        entity.HeightInDots = printer.HeightInDots;
        entity.CreatedAt = printer.CreatedAt;
        entity.ListenTcpPortNumber = printer.ListenTcpPortNumber;
        entity.EmulateBufferCapacity = printer.EmulateBufferCapacity;
        entity.BufferDrainRate = printer.BufferDrainRate;
        entity.BufferMaxCapacity = printer.BufferMaxCapacity;
        entity.CreatedFromIp = printer.CreatedFromIp;
        entity.TargetStatus = DomainMapper.ToString(printer.TargetState);
        entity.RuntimeStatusUpdatedAt = null;
        entity.RuntimeStatusError = null;
        entity.IsPinned = printer.IsPinned;
        entity.IsDeleted = printer.IsDeleted;
        entity.LastViewedDocumentId = printer.LastViewedDocumentId;
        entity.LastDocumentReceivedAt = printer.LastDocumentReceivedAt;
    }

    internal static PrinterEntity ToEntity(this Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer);

        var entity = new PrinterEntity();
        printer.MapToEntity(entity);
        return entity;
    }

    internal static Printer ToDomain(this PrinterEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new Printer(
            entity.Id,
            entity.OwnerWorkspaceId,
            entity.DisplayName,
            DomainMapper.ParseProtocol(entity.Protocol),
            entity.WidthInDots,
            entity.HeightInDots,
            entity.CreatedAt,
            entity.CreatedFromIp,
            entity.ListenTcpPortNumber,
            entity.EmulateBufferCapacity,
            entity.BufferDrainRate,
            entity.BufferMaxCapacity,
            DomainMapper.ParsePrinterTargetState(entity.TargetStatus),
            null,
            null,
            entity.IsPinned,
            entity.IsDeleted,
            entity.LastViewedDocumentId,
            entity.LastDocumentReceivedAt);
    }
}
