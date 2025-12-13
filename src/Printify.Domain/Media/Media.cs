namespace Printify.Domain.Media;

/// <param name="Id">Unique identifier for the media object.</param>
/// <param name="OwnerWorkspaceId">Identifier of the workspace that owns the Media</param>
/// <param name="CreatedAt">Timestamp when the media was created.</param>
/// <param name="IsDeleted">Indicates whether the media has been marked as deleted.</param>
/// <param name="ContentType">MIME type, e.g. "image/png".</param>
/// <param name="Length">Size in bytes.</param>
/// <param name="Sha256Checksum">SHA-256 checksum as lowercase hexadecimal string.</param>
/// <param name="Url">URL where the media can be accessed.</param>
public sealed record Media(
    Guid Id,
    Guid? OwnerWorkspaceId,
    DateTimeOffset CreatedAt,
    bool IsDeleted,
    string ContentType,
    long Length,
    string Sha256Checksum,
    string Url
) : BaseDomainEntity(Id, CreatedAt, IsDeleted);