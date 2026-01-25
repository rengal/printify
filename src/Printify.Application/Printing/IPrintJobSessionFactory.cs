using Printify.Domain.PrintJobs;

namespace Printify.Application.Printing;

public interface IPrintJobSessionFactory
{
    Task<IPrintJobSession> Create(PrintJob job, IPrinterChannel channel, CancellationToken ct);
}
