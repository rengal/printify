using Printify.Domain.Printers;
using Printify.Infrastructure.Persistence.Entities.Printers;

namespace Printify.Infrastructure.Mapping;

internal static class PrinterEntityMapper
{
    internal static PrinterEntity ToEntity(this Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer);

        return new PrinterEntity
        {
            Id = printer.Id,
            OwnerUserId = printer.OwnerUserId,
            OwnerAnonymousSessionId = printer.OwnerAnonymousSessionId,
            DisplayName = printer.DisplayName,
            Protocol = printer.Protocol,
            WidthInDots = printer.WidthInDots,
            HeightInDots = printer.HeightInDots,
            CreatedAt = printer.CreatedAt,
            CreatedFromIp = printer.CreatedFromIp,
            ListenTcpPortNumber = printer.ListenTcpPortNumber,
            IsDeleted = printer.IsDeleted
        };
    }

    internal static Printer ToDomain(this PrinterEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new Printer(
            entity.Id,
            entity.OwnerUserId,
            entity.OwnerAnonymousSessionId,
            entity.DisplayName,
            entity.Protocol,
            entity.WidthInDots,
            entity.HeightInDots,
            entity.CreatedAt,
            entity.CreatedFromIp,
            entity.ListenTcpPortNumber,
            entity.IsDeleted);
    }
}


