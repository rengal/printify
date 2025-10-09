using Printify.Web.Contracts.Media;

namespace Printify.Web.Contracts.Documents.Elements;

/// <summary>
/// Base type for raster images with shared geometry across content or descriptors.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Width">Image width in printer dots.</param>
/// <param name="Height">Image height in printer dots.</param>
/// <param name="Ref">Media payload (bytes plus metadata) for the raster image.</param>
public record RasterImage(
    int Sequence,
    int Width,
    int Height,
    MediaRef Ref)
    : PrintingElement(Sequence);
