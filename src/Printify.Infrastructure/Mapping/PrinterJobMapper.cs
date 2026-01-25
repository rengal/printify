using Printify.Domain.PrintJobs;
using Printify.Infrastructure.Persistence.Entities.PrinterJobs;

namespace Printify.Infrastructure.Mapping;

/// <summary>
/// Mapper for PrintJob domain to persistence entity conversion.
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
            Protocol = EnumMapper.ToString(job.Settings.Protocol),
            WidthInDots = job.Settings.WidthInDots,
            HeightInDots = job.Settings.HeightInDots,
            ClientAddress = job.ClientAddress
        };
    }
}
