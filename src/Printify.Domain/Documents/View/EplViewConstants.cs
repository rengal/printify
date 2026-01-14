namespace Printify.Domain.Documents.View;

/// <summary>
/// EPL view constants used for layout calculations.
/// EPL fonts are scalable, these are the base sizes for the standard fonts.
/// Font 0: 2.1mm (approx 12 dots) width, 2.6mm (approx 15 dots) height
/// Font 1: 1.7mm (approx 10 dots) width, 2.6mm (approx 15 dots) height
/// Font 2: 2.6mm (approx 15 dots) width, 3.5mm (approx 20 dots) height
/// </summary>
public static class EplViewConstants
{
    // Font 0 - standard width
    public const int Font0Width = 12;
    public const int Font0Height = 15;

    // Font 1 - narrow
    public const int Font1Width = 10;
    public const int Font1Height = 15;

    // Font 2 - wide
    public const int Font2Width = 15;
    public const int Font2Height = 20;
}
