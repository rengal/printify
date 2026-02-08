using Printify.Domain.Media;
using DomainMedia = Printify.Domain.Media.Media;

namespace Printify.Infrastructure.Printing.Epl.Commands;

/// <summary>
/// Barcode upload command for EPL protocol.
/// This is a temporary command used during parsing that will be converted to EplPrintBarcode
/// with actual media during finalization.
/// </summary>
/// <param name="X">Horizontal position (in dots).</param>
/// <param name="Y">Vertical position (in dots).</param>
/// <param name="Rotation">Rotation: 0=normal, 1=90°, 2=180°, 3=270°.</param>
/// <param name="Type">Barcode type (e.g., "E30" for EAN-13).</param>
/// <param name="Width">Module width (1-6, typically 2).</param>
/// <param name="Height">Barcode height in dots.</param>
/// <param name="Hri">Human readable interpretation: B=both, N=none, A=above, B=below.</param>
/// <param name="Data">Barcode data/content.</param>
/// <param name="MediaUpload">Media upload containing the barcode image data.</param>
public sealed record EplPrintBarcodeUpload(
    int X,
    int Y,
    int Rotation,
    string Type,
    int Width,
    int Height,
    char Hri,
    string Data,
    MediaUpload MediaUpload) : EplCommand;

/// <summary>
/// Barcode at X,Y position with persisted media.
/// This is the final command after media has been saved during finalization.
/// </summary>
/// <param name="X">Horizontal position (in dots).</param>
/// <param name="Y">Vertical position (in dots).</param>
/// <param name="Rotation">Rotation: 0=normal, 1=90°, 2=180°, 3=270°.</param>
/// <param name="Type">Barcode type (e.g., "E30" for EAN-13).</param>
/// <param name="Width">Module width (1-6, typically 2).</param>
/// <param name="Height">Barcode height in dots.</param>
/// <param name="Hri">Human readable interpretation: B=both, N=none, A=above, B=below.</param>
/// <param name="Data">Barcode data/content.</param>
/// <param name="Media">Persisted media with URL and metadata.</param>
public sealed record EplPrintBarcode(
    int X,
    int Y,
    int Rotation,
    string Type,
    int Width,
    int Height,
    char Hri,
    string Data,
    DomainMedia Media) : EplCommand;
