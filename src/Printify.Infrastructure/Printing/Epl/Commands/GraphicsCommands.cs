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
public abstract record BaseEplRasterImage(int X, int Y, int Width, int Height) : EplCommand;

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
    MediaUpload MediaUpload) : BaseEplRasterImage(X, Y, Width, Height);

/// <summary>
/// Graphic data to print at X,Y position.
/// Command: GW x, y, bytesPerRow, height, [binary data]
/// Alternative: GS x, y, width, height / GE [binary data] / GE
/// </summary>
/// <param name="X">Horizontal position (in dots).</param>
/// <param name="Y">Vertical position (in dots).</param>
/// <param name="Width">Width in dots (number of columns).</param>
/// <param name="Height">Height in dots (number of rows).</param>
/// <param name="Data">Raw graphic data bytes.</param>
public sealed record PrintGraphic(
    int X,
    int Y,
    int Width,
    int Height,
    byte[] Data) : EplCommand;

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
    DomainMedia Media) : BaseEplRasterImage(X, Y, Width, Height);
