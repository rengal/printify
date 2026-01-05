using Printify.Domain.Printers;

namespace Printify.Domain.PrintJobs;

public sealed record PrintJob(
    Guid Id,
    Printer Printer,
    PrinterSettings Settings,
    DateTimeOffset CreatedAt,
    string ClientAddress)
    : BaseDomainEntity(Id, CreatedAt, false);

