using DomainMedia = Printify.Domain.Media.Media;

namespace Printify.Infrastructure.Printing.EscPos.Commands;

/// <summary>
/// ESC/POS barcode symbologies supported by GS k.
/// </summary>
public enum EscPosBarcodeSymbology
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

/// <summary>
/// Label (HRI) positioning options for barcodes driven by GS H (0x1D 0x48).
/// </summary>
public enum EscPosBarcodeLabelPosition
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

/// <summary>
/// Error correction levels supported by QR codes in ESC/POS.
/// </summary>
public enum EscPosQrErrorCorrectionLevel
{
    /// <summary>
    /// Level L (7% recovery).
    /// </summary>
    Low = 0,

    /// <summary>
    /// Level M (15% recovery).
    /// </summary>
    Medium = 1,

    /// <summary>
    /// Level Q (25% recovery).
    /// </summary>
    Quartile = 2,

    /// <summary>
    /// Level H (30% recovery).
    /// </summary>
    High = 3
}

/// <summary>
/// Supported QR code models for ESC/POS GS ( k configuration.
/// </summary>
public enum EscPosQrModel
{
    /// <summary>
    /// Model 1 QR symbol.
    /// </summary>
    Model1 = 1,

    /// <summary>
    /// Model 2 QR symbol (most common).
    /// </summary>
    Model2 = 2,

    /// <summary>
    /// Micro QR symbol.
    /// </summary>
    Micro = 3
}

/// <summary>
/// Renders a one-dimensional barcode using the GS k command family.
/// </summary>
/// <param name="Symbology">Selected barcode symbology.</param>
/// <param name="Data">Raw data payload to encode.</param>
public sealed record EscPosPrintBarcodeUpload(
    EscPosBarcodeSymbology Symbology,
    string Data)
    : EscPosCommand;

/// <summary>
/// Renders a one-dimensional barcode using the GS k command family.
/// </summary>
/// <param name="Symbology">Selected barcode symbology.</param>
/// <param name="Data">Raw data payload to encode.</param>
public sealed record EscPosPrintBarcode(
    EscPosBarcodeSymbology Symbology,
    string Data,
    int Width,
    int Height,
    DomainMedia Media)
    : EscPosCommand;

/// <summary>
/// Configures the height of subsequent barcodes using GS h (0x1D 0x68).
/// </summary>
/// <param name="HeightInDots">Barcode height in dots.</param>
public sealed record EscPosSetBarcodeHeight(int HeightInDots) : EscPosCommand;

/// <summary>
/// Selects the placement of human-readable barcode labels via GS H (0x1D 0x48).
/// </summary>
/// <param name="Position">Desired label positioning.</param>
public sealed record EscPosSetBarcodeLabelPosition(EscPosBarcodeLabelPosition Position)
    : EscPosCommand;

/// <summary>
/// Configures the module width (basic bar width) for subsequent barcodes using GS w (0x1D 0x77).
/// </summary>
/// <param name="ModuleWidth">Module width in device units (typically dots).</param>
public sealed record EscPosSetBarcodeModuleWidth(int ModuleWidth) : EscPosCommand;

/// <summary>
/// Emits a QR code print request using the last stored payload.
/// </summary>
public sealed record EscPosPrintQrCodeUpload : EscPosCommand;

/// <summary>
/// Emits a QR code print request using the last stored payload.
/// </summary>
public sealed record EscPosPrintQrCode(
    string Data,
    int Width,
    int Height,
    DomainMedia Media) : EscPosCommand;

/// <summary>
/// Stores QR code data into the printer memory using GS ( k.
/// </summary>
/// <param name="Content">Payload to be encoded into the QR symbol.</param>
public sealed record EscPosStoreQrData(string Content) : EscPosCommand;

/// <summary>
/// Selects the QR error correction level for subsequent symbols via GS ( k.
/// </summary>
/// <param name="Level">Chosen QR error correction level.</param>
public sealed record EscPosSetQrErrorCorrection(EscPosQrErrorCorrectionLevel Level)
    : EscPosCommand;

/// <summary>
/// Configures the QR code model for subsequent GS ( k.
/// </summary>
/// <param name="Model">Selected QR code model.</param>
public sealed record EscPosSetQrModel(EscPosQrModel Model) : EscPosCommand;

/// <summary>
/// Sets the module size (dot width) for QR codes via GS ( k.
/// </summary>
/// <param name="ModuleSize">Width of a single QR module in dots.</param>
public sealed record EscPosSetQrModuleSize(int ModuleSize) : EscPosCommand;
