namespace Printify.Contracts.Configuration;

/// <summary>
/// Root application configuration exposed to components via dependency injection.
/// </summary>
public interface IAppConfiguration
{
    ListenerConfiguration Listener { get; }

    PageConfiguration Page { get; }

    StorageConfiguration Storage { get; }

    /// <summary>
    /// Simulated processing throughput expressed as bytes per second. When zero or negative, simulation is disabled.
    /// </summary>
    double BytesPerSecond { get; }

    /// <summary>
    /// Maximum size of the simulated printer buffer in bytes. When zero or negative, overflow simulation is disabled.
    /// </summary>
    int MaxBufferSize { get; }
}
