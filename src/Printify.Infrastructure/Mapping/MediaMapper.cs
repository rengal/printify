using Printify.Infrastructure.Persistence.Entities.Documents;

namespace Printify.Infrastructure.Mapping;

/// <summary>
/// Bidirectional mapper between Media domain and persistence entities.
/// </summary>
internal static class MediaMapper
{
    internal static DocumentMediaEntity ToEntity(Domain.Media.Media media)
    {
        ArgumentNullException.ThrowIfNull(media);

        return new DocumentMediaEntity
        {
            Id = media.Id,
            OwnerWorkspaceId = media.OwnerWorkspaceId,
            CreatedAt = media.CreatedAt,
            IsDeleted = media.IsDeleted,
            ContentType = media.ContentType,
            Length = media.Length,
            Checksum = media.Sha256Checksum,
            FileName = media.FileName,
            Url = media.Url
        };
    }

    internal static Domain.Media.Media ToDomain(this DocumentMediaEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new Domain.Media.Media(
            entity.Id,
            entity.OwnerWorkspaceId,
            entity.CreatedAt,
            entity.IsDeleted,
            entity.ContentType,
            entity.Length,
            entity.Checksum,
            entity.FileName,
            entity.Url);
    }
}
