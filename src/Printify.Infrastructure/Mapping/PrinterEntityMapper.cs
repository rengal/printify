using Printify.Domain.Printers;
using Printify.Infrastructure.Persistence.Entities.Printers;

namespace Printify.Infrastructure.Mapping;

internal static class PrinterEntityMapper
{
    internal static void MapToEntity(this Printer printer, PrinterSettings settings, PrinterEntity entity)
    {
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(entity);
        entity.Id = printer.Id;
        entity.OwnerWorkspaceId = printer.OwnerWorkspaceId;
        entity.DisplayName = printer.DisplayName;
        entity.CreatedAt = printer.CreatedAt;
        entity.CreatedFromIp = printer.CreatedFromIp;
        entity.Settings.Protocol = DomainMapper.ToString(settings.Protocol);
        entity.Settings.WidthInDots = settings.WidthInDots;
        entity.Settings.HeightInDots = settings.HeightInDots;
        entity.Settings.ListenTcpPortNumber = settings.ListenTcpPortNumber;
        entity.Settings.EmulateBufferCapacity = settings.EmulateBufferCapacity;
        entity.Settings.BufferDrainRate = settings.BufferDrainRate;
        entity.Settings.BufferMaxCapacity = settings.BufferMaxCapacity;
        entity.IsPinned = printer.IsPinned;
        entity.IsDeleted = printer.IsDeleted;
        entity.LastViewedDocumentId = printer.LastViewedDocumentId;
        entity.LastDocumentReceivedAt = printer.LastDocumentReceivedAt;
    }

    internal static PrinterEntity ToEntity(this Printer printer, PrinterSettings settings)
    {
        ArgumentNullException.ThrowIfNull(printer);
        ArgumentNullException.ThrowIfNull(settings);

        var entity = new PrinterEntity();
        printer.MapToEntity(settings, entity);
        return entity;
    }

    internal static Printer ToDomain(this PrinterEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new Printer(
            entity.Id,
            entity.OwnerWorkspaceId,
            entity.DisplayName,
            entity.CreatedAt,
            entity.CreatedFromIp,
            null, // Runtime status timestamp is computed and not persisted.
            entity.IsPinned,
            entity.IsDeleted,
            entity.LastViewedDocumentId,
            entity.LastDocumentReceivedAt);
    }

    internal static PrinterSettings ToSettings(this PrinterEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new PrinterSettings(
            DomainMapper.ParseProtocol(entity.Settings.Protocol),
            entity.Settings.WidthInDots,
            entity.Settings.HeightInDots,
            entity.Settings.ListenTcpPortNumber,
            entity.Settings.EmulateBufferCapacity,
            entity.Settings.BufferDrainRate,
            entity.Settings.BufferMaxCapacity);
    }
}
