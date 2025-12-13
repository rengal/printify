namespace Printify.Domain.Media;

/// <summary>
/// Represents media data for upload operations only.
/// </summary>
/// <param name="ContentType">MIME type, e.g. "image/png".</param>
/// <param name="Content">Binary content of the media.</param>
public sealed record MediaUpload(
    string ContentType,
    ReadOnlyMemory<byte> Content
);
