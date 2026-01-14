using System;
using Microsoft.Extensions.DependencyInjection;
using Printify.Application.Printing;
using Printify.Domain.Printers;
using Printify.Domain.PrintJobs;
using Printify.Domain.Services;
using Printify.Infrastructure.Printing.Epl;
using Printify.Infrastructure.Printing.EscPos;

namespace Printify.Infrastructure.Printing.Factories;

public class PrintJobSessionFactory(
    IPrinterBufferCoordinator bufferCoordinator,
    IClockFactory clockFactory,
    EscPosCommandTrieProvider escPosCommandTrieProvider,
    EplCommandTrieProvider eplCommandTrieProvider,
    IServiceScopeFactory scopeFactory)
    : IPrintJobSessionFactory
{
    public Task<IPrintJobSession> Create(PrintJob job, IPrinterChannel channel, CancellationToken ct)
    {
        var protocol = channel.Settings.Protocol;
        if (protocol == Protocol.EscPos)
        {
            return Task.FromResult<IPrintJobSession>(
                new EscPosPrintJobSession(
                    bufferCoordinator,
                    clockFactory,
                    job,
                    channel,
                    escPosCommandTrieProvider,
                    scopeFactory));
        }

        if (protocol == Protocol.Epl)
        {
            return Task.FromResult<IPrintJobSession>(
                new EplPrintJobSession(
                    bufferCoordinator,
                    clockFactory,
                    job,
                    channel,
                    eplCommandTrieProvider,
                    scopeFactory));
        }

        throw new ArgumentOutOfRangeException(nameof(protocol));
    }
}
