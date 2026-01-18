namespace Printify.Infrastructure.Printing.Epl.Commands;

/// <summary>
/// Barcode at X,Y position.
/// Command: B x, y, rotation, type, width, height, hri, "data"
/// </summary>
/// <param name="X">Horizontal position (in dots).</param>
/// <param name="Y">Vertical position (in dots).</param>
/// <param name="Rotation">Rotation: 0=normal, 1=90°, 2=180°, 3=270°.</param>
/// <param name="Type">Barcode type (e.g., "E30" for EAN-13).</param>
/// <param name="Width">Module width (1-6, typically 2).</param>
/// <param name="Height">Barcode height in dots.</param>
/// <param name="Hri">Human readable interpretation: B=both, N=none, A=above, B=below.</param>
/// <param name="Data">Barcode data/content.</param>
public sealed record PrintBarcode(
    int X,
    int Y,
    int Rotation,
    string Type,
    int Width,
    int Height,
    char Hri,
    string Data) : EplCommand;
