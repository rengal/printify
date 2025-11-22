namespace Printify.Domain.Media;

/// <param name="Id">Unique identifier for the media object.</param>
/// <param name="CreatedAt">Timestamp when the media was created.</param>
/// <param name="IsDeleted">Indicates whether the media has been marked as deleted.</param>
/// <param name="ContentType">MIME type, e.g. "image/png".</param>
/// <param name="Length">Size in bytes, if known.</param>
/// <param name="Checksum">Hash or checksum of the media content for integrity verification.</param>
/// <param name="Url">URL where the media can be accessed.</param>
public sealed record Media(
    Guid Id,
    DateTimeOffset CreatedAt,
    bool IsDeleted,
    string ContentType,
    long? Length,
    string? Checksum,
    string Url
) : BaseDomainEntity(Id, CreatedAt, IsDeleted);