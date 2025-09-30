namespace Printify.Contracts.Documents.Elements;

using Printify.Contracts.Media;

/// <summary>
/// Base type for raster images with shared geometry across content or descriptors.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Width">Image width in printer dots.</param>
/// <param name="Height">Image height in printer dots.</param>
public abstract record BaseRasterImage(
    int Sequence,
    int Width,
    int Height)
    : PrintingElement(Sequence);

/// <summary>
/// Raster image that carries the media payload directly in the element.
/// </summary>
/// <param name="Media">Media payload (bytes plus metadata) for the raster image.</param>
public sealed record RasterImageContent(
    int Sequence,
    int Width,
    int Height,
    MediaContent Media)
    : BaseRasterImage(Sequence, Width, Height);

/// <summary>
/// Raster image that references media via a descriptor (URL plus metadata).
/// </summary>
/// <param name="Media">Descriptor describing where the raster image bytes can be retrieved.</param>
public sealed record RasterImageDescriptor(
    int Sequence,
    int Width,
    int Height,
    MediaDescriptor Media)
    : BaseRasterImage(Sequence, Width, Height);
