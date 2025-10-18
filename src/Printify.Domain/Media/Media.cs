namespace Printify.Domain.Media;

/// <summary>
/// Represents a persisted media asset.
/// </summary>
/// <param name="Id">Identifier of the media asset.</param>
/// <param name="CreatedAt">Timestamp when the media was stored.</param>
/// <param name="IsDeleted">Soft-delete marker for the media asset.</param>
/// <param name="Meta">Associated media metadata.</param>
/// <param name="ContentUri">Locator for the stored content.</param>
public sealed record Media(
    Guid Id,
    DateTimeOffset CreatedAt,
    bool IsDeleted,
    MediaMeta Meta,
    string ContentUri)
    : BaseDomainEntity(Id, CreatedAt, IsDeleted);
