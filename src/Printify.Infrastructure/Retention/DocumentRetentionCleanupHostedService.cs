using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Printify.Domain.Config;

namespace Printify.Infrastructure.Retention;

public sealed class DocumentRetentionCleanupHostedService(
    DocumentRetentionCleanupService cleanupService,
    IOptions<DocumentCleanupOptions> cleanupOptions,
    ILogger<DocumentRetentionCleanupHostedService> logger)
    : BackgroundService
{
    private const int DefaultIntervalMinutes = 60;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = cleanupOptions.Value;
        if (!options.Enabled)
        {
            logger.LogInformation("Document retention cleanup is disabled");
            return;
        }

        var startupDelay = TimeSpan.FromSeconds(Math.Max(0, options.StartupDelaySeconds));
        if (startupDelay > TimeSpan.Zero)
        {
            await Task.Delay(startupDelay, stoppingToken).ConfigureAwait(false);
        }

        await RunCleanupAsync(stoppingToken).ConfigureAwait(false);

        using var timer = new PeriodicTimer(GetInterval(options));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await RunCleanupAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            await cleanupService.RunOnceAsync(DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Document retention cleanup failed");
        }
    }

    private static TimeSpan GetInterval(DocumentCleanupOptions options)
    {
        var minutes = options.IntervalMinutes > 0
            ? options.IntervalMinutes
            : DefaultIntervalMinutes;

        return TimeSpan.FromMinutes(minutes);
    }
}
