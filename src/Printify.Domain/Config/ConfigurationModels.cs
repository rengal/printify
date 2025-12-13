using Microsoft.Extensions.Configuration;

namespace Printify.Domain.Config;

/// <summary>
/// Listener endpoint configuration.
/// </summary>
public sealed class ListenerOptions
{
    [ConfigurationKeyName("idle_timeout")]
    public int IdleTimeoutInMs { get; init; } = 2000;
}

/// <summary>
/// Storage configuration (provider, file paths, retention).
/// </summary>
public sealed class Storage
{
    [ConfigurationKeyName("database_path")]
    public string DatabasePath { get; init; } = "";
    
    [ConfigurationKeyName("media_root_path")]
    public string MediaRootPath { get; init; } = string.Empty;
}
