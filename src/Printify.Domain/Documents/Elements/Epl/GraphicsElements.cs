namespace Printify.Domain.Documents.Elements.Epl;

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
    byte[] Data) : PrintingElement;
