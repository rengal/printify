using Printify.Application.Interfaces;
using Printify.Application.Printing;

namespace Printify.Web.Infrastructure;

internal sealed class PrinterListenerBootstrapper(
    IPrinterRepository printerRepository,
    IPrinterListenerOrchestrator orchestrator,
    IPrinterListenerFactory listenerFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var printers = await printerRepository.ListAllAsync(ct);

        foreach (var printer in printers)
        {
            var listener = listenerFactory.Create(printer);
            await orchestrator.AddListenerAsync(printer, ct).ConfigureAwait(false);
        }
    }

    public async Task StopAsync(CancellationToken ct)
    {
        var printers = await printerRepository.ListAllAsync(ct);
        foreach (var printer in printers)
        {
            await orchestrator.RemoveListenerAsync(printer, ct).ConfigureAwait(false);
        }
    }
}
