namespace Printify.Contracts.Services;

/// <summary>
/// Lifecycle control for the TCP listener service.
/// Implementations are also expected to be registered as <see cref="Microsoft.Extensions.Hosting.IHostedService"/>.
/// </summary>
public interface IListenerService
{
    /// <summary>
    /// Start the listener. Implementations should begin accepting connections.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stop the listener. Implementations should stop accepting connections and clean up resources.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken);
}
