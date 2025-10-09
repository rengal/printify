namespace Printify.Domain.Media;

/// <summary>
/// Metadata + locator for a media payload (no bytes).
/// Ideal for HTML/API responses where the client fetches bytes via <see cref="Url"/>.
/// </summary>
/// <param name="Meta">Shared media metadata.</param>
/// <param name="Url">Absolute or app-relative URL to retrieve the media bytes.</param>
public sealed record MediaDescriptor(
    MediaMeta Meta,
    string Url
);