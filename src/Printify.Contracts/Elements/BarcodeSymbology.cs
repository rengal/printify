namespace Printify.Contracts.Elements;

/// <summary>
/// ESC/POS barcode symbologies supported by GS k.
/// </summary>
public enum BarcodeSymbology
{
    /// <summary>
    /// US UPC-A.
    /// </summary>
    UpcA = 0,

    /// <summary>
    /// US UPC-E.
    /// </summary>
    UpcE = 1,

    /// <summary>
    /// JAN (EAN) 13-digit barcode.
    /// </summary>
    Ean13 = 2,

    /// <summary>
    /// JAN (EAN) 8-digit barcode.
    /// </summary>
    Ean8 = 3,

    /// <summary>
    /// Code 39 symbology.
    /// </summary>
    Code39 = 4,

    /// <summary>
    /// Interleaved 2 of 5.
    /// </summary>
    Itf = 5,

    /// <summary>
    /// Codabar (NW-7).
    /// </summary>
    Codabar = 6,

    /// <summary>
    /// Code 93 symbology.
    /// </summary>
    Code93 = 7,

    /// <summary>
    /// Code 128 symbology.
    /// </summary>
    Code128 = 8
}
