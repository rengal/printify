using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Printify.Application.Printing;
using Printify.Infrastructure.Persistence;

namespace Printify.Web.Infrastructure;

internal sealed class PrinterListenerBootstrapper(
    IServiceScopeFactory scopeFactory,
    IPrinterListenerOrchestrator orchestrator,
    IPrinterListenerFactory listenerFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PrintifyDbContext>();

            var printers = await dbContext.Printers
                .Where(printer => !printer.IsDeleted)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var entity in printers)
            {
                var listener = listenerFactory.Create(entity.Id, entity.ListenTcpPortNumber);
                await orchestrator.AddListenerAsync(entity.Id, listener, cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            // Database might not be initialized yet (e.g., during tests). Listeners can be registered later.
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PrintifyDbContext>();

            var printerIds = await dbContext.Printers
                .Select(printer => printer.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var printerId in printerIds)
            {
                try
                {
                    await orchestrator.RemoveListenerAsync(printerId, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // Ignore teardown errors during shutdown.
                }
            }
        }
        catch
        {
            // Ignore teardown errors during shutdown.
        }
    }
}
