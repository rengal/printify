using Printify.Contracts.Configuration;

namespace Printify.Config;

/// <summary>
/// Concrete configuration instance that can be populated via configuration providers.
/// </summary>
public sealed class AppConfiguration : IAppConfiguration
{
    public ListenerConfiguration Listener { get; init; } = new();

    public PageConfiguration Page { get; init; } = new();

    public StorageConfiguration Storage { get; init; } = new();

    /// <inheritdoc />
    public double BytesPerSecond { get; init; }

    /// <inheritdoc />
    public int MaxBufferSize { get; init; }
}
