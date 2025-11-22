namespace Printify.Infrastructure.Config;

/// <summary>
/// Provides configuration for repository implementations backed by SQLite.
/// </summary>
public sealed class MediaStorageOptions
{
    public string RootPath { get; set; } = string.Empty;
}
