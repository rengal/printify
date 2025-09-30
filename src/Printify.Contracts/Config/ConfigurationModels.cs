using Microsoft.Extensions.Configuration;

namespace Printify.Contracts.Config;

/// <summary>
/// Listener endpoint configuration.
/// </summary>
public sealed class ListenerOptions
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
/// Configuration for the simulated printer buffer: throughput, busy threshold, and capacity.
/// </summary>
public sealed class BufferOptions
{
    /// <summary>
    /// Simulated drain rate of the buffer, expressed in <b>bytes per second</b>.
    /// When set to 0 or <c>null</c>, the buffer drains instantly (no throttling).
    /// </summary>
    public double? DrainRate { get; init; }

    /// <summary>
    /// Threshold at which the buffer is considered "busy", in <b>bytes</b>.
    /// Useful for simulating back-pressure or warnings.
    /// If <c>null</c>, the buffer never enters a busy state.
    /// </summary>
    public int? BusyThreshold { get; init; }

    /// <summary>
    /// Maximum capacity of the buffer, in <b>bytes</b>.
    /// When set to 0, negative, or <c>null</c>, overflow simulation is disabled.
    /// </summary>
    public int? MaxCapacity { get; init; }
}
