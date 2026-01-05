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

            var printerSettings = await printerRepository.GetSettingsAsync(printer.Id, ct).ConfigureAwait(false);
            // Settings are persisted separately; missing settings indicate a data integrity issue.
            if (printerSettings is null)
            {
                throw new InvalidOperationException($"Settings for printer {printer.Id} are missing.");
            }
            var operationalFlags = await printerRepository.GetOperationalFlagsAsync(printer.Id, ct).ConfigureAwait(false);
            // Default to Started to preserve legacy behavior when no operational flags exist yet.
            var targetState = operationalFlags?.TargetState ?? PrinterTargetState.Started;
            // Only start listeners for printers marked as Started.
            if (targetState == PrinterTargetState.Started)
            {
                await orchestrator.AddListenerAsync(printer, printerSettings, targetState, ct).ConfigureAwait(false);
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

            var operationalFlags = await printerRepository.GetOperationalFlagsAsync(printer.Id, ct).ConfigureAwait(false);
            // Default to Stopped to avoid re-starting listeners during shutdown.
            var targetState = operationalFlags?.TargetState ?? PrinterTargetState.Stopped;
            await orchestrator.RemoveListenerAsync(printer, targetState, ct).ConfigureAwait(false);
        }
    }
}
