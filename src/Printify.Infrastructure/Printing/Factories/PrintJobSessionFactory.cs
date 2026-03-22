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
    public Task<IPrintJobSession> Create(PrintJob job, IPrinterChannel channel, CancellationToken ct, bool skipBufferCheck = false)
    {
        var protocol = channel.Settings.Protocol;
        var effectiveScopeFactory = skipBufferCheck ? null : scopeFactory;
        if (protocol == Protocol.EscPos)
        {
            return Task.FromResult<IPrintJobSession>(
                new EscPosPrintJobSession(
                    bufferCoordinator,
                    clockFactory,
                    job,
                    channel,
                    escPosCommandTrieProvider,
                    effectiveScopeFactory));
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
                    effectiveScopeFactory));
        }

        throw new ArgumentOutOfRangeException(nameof(protocol));
    }
}
