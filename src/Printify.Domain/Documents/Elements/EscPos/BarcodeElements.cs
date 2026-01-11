using Printify.Domain.Media;
using DomainMedia = Printify.Domain.Media.Media;

namespace Printify.Domain.Documents.Elements.EscPos;

/// <summary>
/// Renders a one-dimensional barcode using the GS k command family.
/// </summary>
/// <param name="Symbology">Selected barcode symbology.</param>
/// <param name="Data">Raw data payload to encode.</param>
public sealed record PrintBarcodeUpload(
    BarcodeSymbology Symbology,
    string Data)
    : PrintingElement;

/// <summary>
/// Renders a one-dimensional barcode using the GS k command family.
/// </summary>
/// <param name="Symbology">Selected barcode symbology.</param>
/// <param name="Data">Raw data payload to encode.</param>
public sealed record PrintBarcode(
    BarcodeSymbology Symbology,
    string Data,
    int Width,
    int Height,
    DomainMedia Media)
    : PrintingElement;

/// <summary>
/// Configures the height of subsequent barcodes using GS h (0x1D 0x68).
/// </summary>
/// <param name="HeightInDots">Barcode height in dots.</param>
public sealed record SetBarcodeHeight(int HeightInDots) : NonPrintingElement;

/// <summary>
/// Selects the placement of human-readable barcode labels via GS H (0x1D 0x48).
/// </summary>
/// <param name="Position">Desired label positioning.</param>
public sealed record SetBarcodeLabelPosition(BarcodeLabelPosition Position)
    : NonPrintingElement;

/// <summary>
/// Configures the module width (basic bar width) for subsequent barcodes using GS w (0x1D 0x77).
/// </summary>
/// <param name="ModuleWidth">Module width in device units (typically dots).</param>
public sealed record SetBarcodeModuleWidth(int ModuleWidth) : NonPrintingElement;

/// <summary>
/// Emits a QR code print request using the last stored payload.
/// </summary>
public sealed record PrintQrCodeUpload : PrintingElement;

/// <summary>
/// Emits a QR code print request using the last stored payload.
/// </summary>
public sealed record PrintQrCode(
    string Data,
    int Width,
    int Height,
    DomainMedia Media) : PrintingElement;

/// <summary>
/// Stores QR code data into the printer memory using GS ( k.
/// </summary>
/// <param name="Content">Payload to be encoded into the QR symbol.</param>
public sealed record StoreQrData(string Content) : NonPrintingElement;

/// <summary>
/// Selects the QR error correction level for subsequent symbols via GS ( k.
/// </summary>
/// <param name="Level">Chosen QR error correction level.</param>
public sealed record SetQrErrorCorrection(QrErrorCorrectionLevel Level)
    : NonPrintingElement;

/// <summary>
/// Configures the QR code model for subsequent GS ( k.
/// </summary>
/// <param name="Model">Selected QR code model.</param>
public sealed record SetQrModel(QrModel Model) : NonPrintingElement;

/// <summary>
/// Sets the module size (dot width) for QR codes via GS ( k.
/// </summary>
/// <param name="ModuleSize">Width of a single QR module in dots.</param>
public sealed record SetQrModuleSize(int ModuleSize) : NonPrintingElement;
