using Printify.Domain;

namespace Printify.Domain.PrintJobs;

public sealed record PrintJob(
    Guid Id,
    Guid PrinterId,
    string DisplayName,
    string Protocol,
    int WidthInDots,
    int? HeightInDots,
    DateTimeOffset CreatedAt,
    string CreatedFromIp,
    int ListenTcpPortNumber,
    IPrintJobState State,
    bool IsDeleted)
    : BaseDomainEntity(Id, CreatedAt, IsDeleted);

