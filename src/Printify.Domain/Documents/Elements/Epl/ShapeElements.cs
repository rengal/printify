namespace Printify.Domain.Documents.Elements.Epl;

/// <summary>
/// Draw line or box from (x1,y1) to (x2,y2).
/// Command: X x1, y1, thickness, x2, y2
/// </summary>
/// <param name="X1">Starting horizontal position (in dots).</param>
/// <param name="Y1">Starting vertical position (in dots).</param>
/// <param name="Thickness">Line thickness in dots.</param>
/// <param name="X2">Ending horizontal position (in dots).</param>
/// <param name="Y2">Ending vertical position (in dots).</param>
public sealed record DrawLine(
    int X1,
    int Y1,
    int Thickness,
    int X2,
    int Y2) : PrintingElement;
