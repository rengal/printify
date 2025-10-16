namespace Printify.Infrastructure.Config;

/// <summary>
/// Provides configuration for repository implementations backed by SQLite.
/// </summary>
public sealed class RepositoryOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}
