using Printify.Domain.Media;
using DomainMedia = Printify.Domain.Media.Media;

namespace Printify.Infrastructure.Printing.Epl.Commands;

/// <summary>
/// Base type for EPL raster images with shared geometry.
/// </summary>
/// <param name="X">Horizontal position (in dots).</param>
/// <param name="Y">Vertical position (in dots).</param>
/// <param name="Width">Width in dots (number of columns).</param>
/// <param name="Height">Height in dots (number of rows).</param>
public abstract record EplBaseRasterImage(int X, int Y, int Width, int Height) : EplCommand;

/// <summary>
/// Raster image upload command for EPL protocol.
/// This is a temporary command used during parsing that will be converted to EplRasterImage
/// with actual media during finalization.
/// </summary>
/// <param name="X">Horizontal position (in dots).</param>
/// <param name="Y">Vertical position (in dots).</param>
/// <param name="Width">Width in dots (number of columns).</param>
/// <param name="Height">Height in dots (number of rows).</param>
/// <param name="MediaUpload">Media upload containing the raster image data.</param>
public sealed record EplRasterImageUpload(
    int X,
    int Y,
    int Width,
    int Height,
    MediaUpload MediaUpload) : EplBaseRasterImage(X, Y, Width, Height);

/// <summary>
/// Raster image with persisted media.
/// This is the final command after media has been saved during finalization.
/// </summary>
/// <param name="X">Horizontal position (in dots).</param>
/// <param name="Y">Vertical position (in dots).</param>
/// <param name="Width">Width in dots (number of columns).</param>
/// <param name="Height">Height in dots (number of rows).</param>
/// <param name="Media">Persisted media with URL and metadata.</param>
public sealed record EplRasterImage(
    int X,
    int Y,
    int Width,
    int Height,
    DomainMedia Media) : EplBaseRasterImage(X, Y, Width, Height);
