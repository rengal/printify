using Microsoft.Extensions.Configuration;

namespace Printify.Contracts.Configuration;

/// <summary>
/// Listener endpoint configuration.
/// </summary>
public sealed class ListenerConfiguration
{
    [ConfigurationKeyName("url")]
    public string Url { get; init; } = string.Empty;
}

/// <summary>
/// Page layout configuration for rendering.
/// </summary>
public sealed class PageConfiguration
{
    [ConfigurationKeyName("width_dots")]
    public int WidthDots { get; init; }
}

/// <summary>
/// Storage configuration (provider, file paths, retention).
/// </summary>
public sealed class StorageConfiguration
{
    public string Provider { get; init; } = string.Empty;

    [ConfigurationKeyName("databasePath")]
    public string DatabasePath { get; init; } = string.Empty;

    [ConfigurationKeyName("retention_days")]
    public int RetentionDays { get; init; }
}
