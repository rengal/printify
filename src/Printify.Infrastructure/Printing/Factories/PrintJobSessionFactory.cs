using Printify.Application.Printing;
using Printify.Domain.PrintJobs;
using Printify.Domain.Services;
using Printify.Infrastructure.Printing.EscPos;

namespace Printify.Infrastructure.Printing.Factories;

public class PrintJobSessionFactory(IClockFactory clockFactory) : IPrintJobSessionFactory
{
    public Task<IPrintJobSession> Create(PrintJob job, IPrinterChannel channel, CancellationToken ct)
    {
        var protocol = channel.Printer.Protocol;
        if (protocol == "escpos")
            return Task.FromResult<IPrintJobSession>(new EscPosPrintJobSession(clockFactory, job, channel));
        throw new ArgumentOutOfRangeException(nameof(protocol));
    }
}
