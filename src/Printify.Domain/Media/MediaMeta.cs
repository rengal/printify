namespace Printify.Domain.Media;

/// <summary>
/// Shared metadata for a media payload.
/// </summary>
/// <param name="ContentType">MIME type, e.g. "image/png".</param>
/// <param name="Length">Size in bytes, if known.</param>
/// <param name="Checksum">Optional integrity tag, e.g. "sha256:abcdef...".</param>
public sealed record MediaMeta(
    string ContentType,
    long? Length,
    string? Checksum
);