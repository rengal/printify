using Microsoft.Extensions.Configuration;

namespace Printify.Contracts.Config;

/// <summary>
/// Listener endpoint configuration.
/// </summary>
public sealed class Listener
{
    [ConfigurationKeyName("url")]
    public string Url { get; init; } = "http://0.0.0.0:9100";

    [ConfigurationKeyName("idle_timeout")]
    public int IdleTimeoutInMs { get; init; } = 2000;
}

/// <summary>
/// Page layout configuration for rendering.
/// </summary>
public sealed class Page
{
    [ConfigurationKeyName("width_dots")]
    public int WidthDots { get; init; }
}

/// <summary>
/// Storage configuration (provider, file paths, retention).
/// </summary>
public sealed class Storage
{
    [ConfigurationKeyName("database_path")]
    public string DatabasePath { get; init; } = "";
    
    [ConfigurationKeyName("blob_path")]
    public string BlobPath { get; init; } = string.Empty;
}

/// <summary>
/// Buffer configuration (drain rate, maximum size
/// </summary>
public sealed class BufferOptions
{
    /// <summary>
    /// Simulated processing throughput expressed as bytes per second. When zero or negative, simulation is disabled.
    /// </summary>
    public double? BytesPerSecond { get; init; } = null;

    /// <summary>
    /// Maximum size of the simulated printer buffer in bytes. When zero or negative, overflow simulation is disabled.
    /// </summary>
    public int? MaxBufferSize { get; init; } = null;
}
