namespace Printify.Domain.Media;

/// <param name="Id">Unique identifier for the media object.</param>
/// <param name="OwnerWorkspaceId">Identifier of the workspace that owns the Media</param>
/// <param name="CreatedAt">Timestamp when the media was created.</param>
/// <param name="IsDeleted">Indicates whether the media has been marked as deleted.</param>
/// <param name="ContentType">MIME type, e.g. "image/png".</param>
/// <param name="Length">Size in bytes.</param>
/// <param name="Sha256Checksum">SHA-256 checksum as lowercase hexadecimal string.</param>
/// <param name="FileName"> Relative path within the storage folder where the media file is physically stored (e.g., "media/2024/image.png").</param>
/// <param name="Url">Relative public URI where the media can be accessed (e.g., "/media/image.png").</param>
public sealed record Media(
    Guid Id,
    Guid? OwnerWorkspaceId,
    DateTimeOffset CreatedAt,
    bool IsDeleted,
    string ContentType,
    long Length,
    string Sha256Checksum,
    string FileName,
    string Url
) : BaseDomainEntity(Id, CreatedAt, IsDeleted)
{
    /// <summary>
    /// Creates a default PNG media object with minimal required fields.
    /// </summary>
    /// <returns>A new Media instance configured for PNG content type with default values.</returns>
    public static Media CreateDefaultPng(int length) =>
        new(
            Id: Guid.Empty,
            OwnerWorkspaceId: null,
            CreatedAt: DateTimeOffset.MinValue,
            IsDeleted: false,
            ContentType: "image/png",
            Length: length,
            Sha256Checksum: string.Empty,
            FileName: string.Empty,
            Url: string.Empty
        );
}
