using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Web.Infrastructure;

internal sealed class PrinterListenerBootstrapper(
    IServiceScopeFactory scopeFactory,
    IPrinterListenerOrchestrator orchestrator) : IPrinterListenerBootstrapper
{
    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var printerRepository = scope.ServiceProvider.GetRequiredService<IPrinterRepository>();
        var printers = await printerRepository.ListAllAsync(ct);

        foreach (var printer in printers)
        {
            if (printer.IsDeleted)
            {
                continue;
            }

            // Only start listeners for printers marked as Started.
            if (printer.TargetState == PrinterTargetState.Started)
            {
                await orchestrator.AddListenerAsync(printer, ct).ConfigureAwait(false);
            }
            else
            {
                await orchestrator.RemoveListenerAsync(printer, ct).ConfigureAwait(false);
            }
        }
    }

    public async Task StopAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var printerRepository = scope.ServiceProvider.GetRequiredService<IPrinterRepository>();
        var printers = await printerRepository.ListAllAsync(ct);
        foreach (var printer in printers)
        {
            if (printer.IsDeleted)
            {
                continue;
            }

            await orchestrator.RemoveListenerAsync(printer, ct).ConfigureAwait(false);
        }
    }
}
