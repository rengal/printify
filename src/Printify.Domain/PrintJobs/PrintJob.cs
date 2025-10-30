using Printify.Domain.Printers;

namespace Printify.Domain.PrintJobs;

public sealed record PrintJob(Guid Id, Printer Printer, DateTimeOffset CreatedAt, string ClientAddress)
    : BaseDomainEntity(Id, CreatedAt, false);

