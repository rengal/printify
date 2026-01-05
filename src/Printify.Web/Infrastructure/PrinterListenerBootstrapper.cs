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

            var realtimeStatus = await printerRepository.GetRealtimeStatusAsync(printer.Id, ct).ConfigureAwait(false);
            // Default to Started to preserve legacy behavior when no realtime status exists yet.
            var targetState = realtimeStatus?.TargetState ?? PrinterTargetState.Started;
            // Only start listeners for printers marked as Started.
            if (targetState == PrinterTargetState.Started)
            {
                await orchestrator.AddListenerAsync(printer, targetState, ct).ConfigureAwait(false);
            }
            else
            {
                await orchestrator.RemoveListenerAsync(printer, targetState, ct).ConfigureAwait(false);
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

            var realtimeStatus = await printerRepository.GetRealtimeStatusAsync(printer.Id, ct).ConfigureAwait(false);
            // Default to Stopped to avoid re-starting listeners during shutdown.
            var targetState = realtimeStatus?.TargetState ?? PrinterTargetState.Stopped;
            await orchestrator.RemoveListenerAsync(printer, targetState, ct).ConfigureAwait(false);
        }
    }
}
