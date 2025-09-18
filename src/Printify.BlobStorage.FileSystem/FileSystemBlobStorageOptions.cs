namespace Printify.BlobStorage.FileSystem;

/// <summary>
/// Configuration settings for <see cref="FileSystemBlobStorage"/>.
/// </summary>
public sealed class FileSystemBlobStorageOptions
{
    /// <summary>
    /// Root directory where blobs should be stored. Must be absolute.
    /// </summary>
    public string? RootPath { get; set; }
}
