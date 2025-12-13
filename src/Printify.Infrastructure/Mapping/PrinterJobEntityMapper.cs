using Printify.Domain.Mapping;
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
            Protocol = DomainMapper.ToString(job.Printer.Protocol),
            WidthInDots = job.Printer.WidthInDots,
            HeightInDots = job.Printer.HeightInDots,
            ClientAddress = job.ClientAddress
        };
    }
}
