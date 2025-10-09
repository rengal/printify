namespace Printify.Web.Contracts.Documents.Elements;

/// <summary>
/// Label (HRI) positioning options for barcodes driven by GS H (0x1D 0x48).
/// </summary>
public enum BarcodeLabelPosition
{
    /// <summary>
    /// Suppress human-readable interpretation text output.
    /// </summary>
    NotPrinted = 0,

    /// <summary>
    /// Render label above the barcode.
    /// </summary>
    Above = 1,

    /// <summary>
    /// Render label below the barcode.
    /// </summary>
    Below = 2,

    /// <summary>
    /// Render label both above and below the barcode.
    /// </summary>
    AboveAndBelow = 3
}
