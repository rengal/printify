namespace Printify.Contracts.Elements;

/// <summary>
/// A raster image (bitmap) in printer dots.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Width">Image width in printer dots.</param>
/// <param name="Height">Image height in printer dots.</param>
/// <param name="Mode">ESC/POS raster mode parameter (m).</param>
/// <param name="BlobId">Identifier in blob storage where the rendered PNG is persisted.</param>
/// <param name="ContentType">Content type stored in blob storage (typically image/png).</param>
/// <param name="ContentLength">Length of the stored blob in bytes.</param>
/// <param name="Checksum">Optional checksum (e.g., SHA256) for integrity validation.</param>
public sealed record RasterImage(
    int Sequence,
    int Width,
    int Height,
    byte Mode,
    string BlobId,
    string ContentType,
    long ContentLength,
    string? Checksum)
    : PrintingElement(Sequence);
