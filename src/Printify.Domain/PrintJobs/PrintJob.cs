namespace Printify.Domain.PrintJobs;

public sealed record PrintJob(
    Guid PrinterId,
    string DisplayName,
    string Protocol,
    int WidthInDots,
    int? HeightInDots,
    DateTimeOffset CreatedAt,
    string CreatedFromIp,
    int ListenTcpPortNumber,
    IPrintJobState State);
