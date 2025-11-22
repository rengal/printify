namespace Printify.Domain.Media;

/// <summary>
/// Represents media data for upload operations only.
/// </summary>
/// <param name="ContentType">MIME type, e.g. "image/png".</param>
/// <param name="Length">Size in bytes, if known.</param>
/// <param name="Checksum">Hash or checksum of the media content for integrity verification.</param>
/// <param name="Content">Binary content of the media.</param>
public sealed record MediaUpload(
    string ContentType,
    long? Length,
    string? Checksum,
    ReadOnlyMemory<byte> Content
);
