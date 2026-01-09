namespace Printify.Domain.Config;

/// <summary>
/// Listener endpoint configuration.
/// </summary>
public sealed class ListenerOptions
{
    //[ConfigurationKeyName("idle_timeout")]
    //public int IdleTimeoutInMs { get; init; } = 2000;
    public string PublicHost { get; init; } = "localhost";
}

/// <summary>
/// Storage configuration (provider, file paths, retention).
/// </summary>
public sealed class Storage
{
    public string DatabasePath { get; init; } = string.Empty;
    
    public string MediaRootPath { get; set; } = string.Empty;
}

/// <summary>
/// Provides configuration for repository implementations backed by SQLite.
/// </summary>
public sealed class RepositoryOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class JwtOptions
{
    public string SecretKey { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public long ExpiresInSeconds { get; set; } = 3600 * 24 * 100; // 100 days
}
