namespace Printify.Domain.Documents.Elements.Epl;

/// <summary>
/// Scalable/rotatable text at X,Y position.
/// Command: A x, y, rotation, font, h-multiplication, v-multiplication, reverse, "text"
/// </summary>
/// <param name="X">Horizontal position (in dots).</param>
/// <param name="Y">Vertical position (in dots).</param>
/// <param name="Rotation">Rotation: 0=normal, 1=90°, 2=180°, 3=270°.</param>
/// <param name="Font">Font selection: 2=font 0, 3=font 1, 4=font 2.</param>
/// <param name="HorizontalMultiplication">Horizontal font multiplication (1-6).</param>
/// <param name="VerticalMultiplication">Vertical font multiplication (1-9).</param>
/// <param name="Reverse">Reverse printing: N=normal, R=reverse.</param>
/// <param name="Text">Text content to print.</param>
public sealed record ScalableText(
    int X,
    int Y,
    int Rotation,
    int Font,
    int HorizontalMultiplication,
    int VerticalMultiplication,
    char Reverse,
    string Text) : PrintingElement;

/// <summary>
/// Draw horizontal line (typically used for underline).
/// Command: LO x, y, thickness, length
/// </summary>
/// <param name="X">Starting horizontal position (in dots).</param>
/// <param name="Y">Vertical position (in dots).</param>
/// <param name="Thickness">Line thickness in dots.</param>
/// <param name="Length">Line length in dots.</param>
public sealed record DrawHorizontalLine(
    int X,
    int Y,
    int Thickness,
    int Length) : PrintingElement;

/// <summary>
/// Print format and feed label.
/// Command: P n (where n is number of copies)
/// </summary>
/// <param name="Copies">Number of copies to print.</param>
public sealed record Print(int Copies) : PrintingElement;
