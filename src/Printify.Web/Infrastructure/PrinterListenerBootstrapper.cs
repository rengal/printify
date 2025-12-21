using Printify.Application.Interfaces;
using Printify.Application.Printing;
using Printify.Domain.Printers;

namespace Printify.Web.Infrastructure;

internal sealed class PrinterListenerBootstrapper(
    IPrinterRepository printerRepository,
    IPrinterListenerOrchestrator orchestrator) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
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
