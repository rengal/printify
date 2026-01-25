using Printify.Domain.Printers;
using Printify.Domain.PrintJobs;
using Printify.Infrastructure.Persistence.Entities.PrinterJobs;

namespace Printify.Infrastructure.Mapping;

/// <summary>
/// Bidirectional mapper between PrintJob domain and persistence entities.
/// </summary>
internal static class PrinterJobMapper
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
            Protocol = EnumMapper.ToString(job.Protocol),
            ClientAddress = job.ClientAddress
        };
    }
}
