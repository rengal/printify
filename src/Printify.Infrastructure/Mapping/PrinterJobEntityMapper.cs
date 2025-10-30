using Printify.Domain.PrintJobs;
using Printify.Infrastructure.Persistence.Entities.PrinterJobs;

namespace Printify.Infrastructure.Mapping;

internal static class PrinterJobEntityMapper
{
    internal static PrintJobEntity ToEntity(this PrintJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return new PrintJobEntity
        {
            Id = job.Id,
            CreatedAt = job.CreatedAt,
            IsDeleted = job.IsDeleted,
            PrinterId = job.Printer.Id,
            DisplayName = job.Printer.DisplayName,
            Protocol = job.Printer.Protocol,
            WidthInDots = job.Printer.WidthInDots,
            HeightInDots = job.Printer.HeightInDots,
            ClientAddress = job.ClientAddress
        };
    }

    internal static PrintJob ToDomain(this PrintJobEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new PrintJob(
            entity.Id,
            entity.PrinterId,
            entity.DisplayName,
            entity.Protocol,
            entity.WidthInDots,
            entity.HeightInDots,
            entity.CreatedAt,
            entity.CreatedFromIp,
            entity.ListenTcpPortNumber);
    }
}
