using System;
using Printify.Application.Printing;
using Printify.Domain.Printers;
using Printify.Domain.PrintJobs;
using Printify.Domain.Services;
using Printify.Infrastructure.Printing.EscPos;

namespace Printify.Infrastructure.Printing.Factories;

public class PrintJobSessionFactory(IClockFactory clockFactory, IEscPosCommandTrieProvider commandTrieProvider)
    : IPrintJobSessionFactory
{
    public Task<IPrintJobSession> Create(PrintJob job, IPrinterChannel channel, CancellationToken ct)
    {
        var protocol = channel.Settings.Protocol;
        if (protocol == Protocol.EscPos)
        {
            return Task.FromResult<IPrintJobSession>(
                new EscPosPrintJobSession(clockFactory, job, channel, commandTrieProvider));
        }

        throw new ArgumentOutOfRangeException(nameof(protocol));
    }
}
