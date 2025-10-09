namespace Printify.Web.Contracts.Media;

/// <summary>
/// Metadata + locator for a media payload (no bytes).
/// </summary>
/// <param name="ContentType">MIME type, e.g. "image/png".</param>
/// <param name="SizeBytes">Size in bytes.</param>
/// <param name="Sha256">Sha256 checksum.</param>
/// <param name="Href">Absolute or app-relative URL to retrieve the media bytes.</param>
public sealed record MediaRef(
    string ContentType,
    long? SizeBytes,
    string? Sha256,
    Uri Href
);
