using System.Text.Json.Serialization;
using EscPosElements = Printify.Domain.Documents.Elements.EscPos;

namespace Printify.Domain.Documents.Elements;

/// <summary>
/// Base type for all document elements produced by tokenizers and consumed by renderers.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "")]
// EscPos Text Elements
[JsonDerivedType(typeof(EscPosElements.AppendText), "appendToLineBuffer")]
[JsonDerivedType(typeof(EscPosElements.PrintAndLineFeed), "flushLineBufferAndFeed")]
[JsonDerivedType(typeof(EscPosElements.LegacyCarriageReturn), "legacyCarriageReturn")]
[JsonDerivedType(typeof(EscPosElements.SelectFont), "setFont")]
[JsonDerivedType(typeof(EscPosElements.SetBoldMode), "setBoldMode")]
[JsonDerivedType(typeof(EscPosElements.SetUnderlineMode), "setUnderlineMode")]
[JsonDerivedType(typeof(EscPosElements.SetReverseMode), "setReverseMode")]
[JsonDerivedType(typeof(EscPosElements.SetJustification), "setJustification")]
[JsonDerivedType(typeof(EscPosElements.SetLineSpacing), "setLineSpacing")]
[JsonDerivedType(typeof(EscPosElements.ResetLineSpacing), "resetLineSpacing")]
[JsonDerivedType(typeof(EscPosElements.SetCodePage), "setCodePage")]
[JsonDerivedType(typeof(EscPosElements.StoredLogo), "storedLogo")]
[JsonDerivedType(typeof(EscPosElements.RasterImageUpload), "rasterImageUpload")]
[JsonDerivedType(typeof(EscPosElements.RasterImage), "rasterImage")]
// EscPos Barcode Elements
[JsonDerivedType(typeof(EscPosElements.PrintBarcodeUpload), "printBarcodeUpload")]
[JsonDerivedType(typeof(EscPosElements.PrintBarcode), "printBarcode")]
[JsonDerivedType(typeof(EscPosElements.SetBarcodeHeight), "setBarcodeHeight")]
[JsonDerivedType(typeof(EscPosElements.SetBarcodeLabelPosition), "setBarcodeLabelPosition")]
[JsonDerivedType(typeof(EscPosElements.SetBarcodeModuleWidth), "setBarcodeModuleWidth")]
[JsonDerivedType(typeof(EscPosElements.PrintQrCodeUpload), "printQrCodeUpload")]
[JsonDerivedType(typeof(EscPosElements.PrintQrCode), "printQrCode")]
[JsonDerivedType(typeof(EscPosElements.StoreQrData), "storeQrData")]
[JsonDerivedType(typeof(EscPosElements.SetQrErrorCorrection), "setQrErrorCorrection")]
[JsonDerivedType(typeof(EscPosElements.SetQrModel), "setQrModel")]
[JsonDerivedType(typeof(EscPosElements.SetQrModuleSize), "setQrModuleSize")]
// EscPos Control Elements
[JsonDerivedType(typeof(EscPosElements.Bell), "bell")]
[JsonDerivedType(typeof(EscPosElements.CutPaper), "pagecut")]
[JsonDerivedType(typeof(EscPosElements.Pulse), "pulse")]
[JsonDerivedType(typeof(EscPosElements.Initialize), "resetPrinter")]
[JsonDerivedType(typeof(EscPosElements.ParseError), "error")]
[JsonDerivedType(typeof(EscPosElements.PrinterError), "printerError")]
[JsonDerivedType(typeof(EscPosElements.GetPrinterStatus), "printerStatus")]
[JsonDerivedType(typeof(EscPosElements.StatusRequest), "statusRequest")]
[JsonDerivedType(typeof(EscPosElements.StatusResponse), "statusResponse")]
public abstract record Element
{
    /// <summary>
    /// Raw command bytes encoded for debugging or UI display.
    /// </summary>
    public string CommandRaw { get; init; } = string.Empty;

    /// <summary>
    /// Length of the command in bytes.
    /// </summary>
    public int LengthInBytes { get; init; }
}

/// <summary>
/// Base type for non-printing control or status events within a document stream.
/// </summary>
public abstract record NonPrintingElement : Element;

/// <summary>
/// Base type for printing (visible) elements that produce output on paper.
/// </summary>
public abstract record PrintingElement : Element;

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

/// <summary>
/// Error correction levels supported by QR codes in ESC/POS.
/// </summary>
public enum QrErrorCorrectionLevel
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
public enum QrModel
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
/// Supported text alignment values for ESC a (justification) commands.
/// </summary>
public enum TextJustification
{
    /// <summary>
    /// Align text to the left margin.
    /// </summary>
    Left = 0,

    /// <summary>
    /// Center text relative to the printable width.
    /// </summary>
    Center = 1,

    /// <summary>
    /// Align text to the right margin.
    /// </summary>
    Right = 2
}

/// <summary>
/// Paper cut operation modes supported by ESC/POS cut commands.
/// </summary>
public enum PagecutMode
{
    /// <summary>
    /// Full paper cut (completely severs the paper).
    /// </summary>
    Full = 0,

    /// <summary>
    /// Partial paper cut (leaves a small connection for easy tear-off).
    /// </summary>
    Partial = 1,

    /// <summary>
    /// Partial cut leaving one point uncut.
    /// </summary>
    PartialOnePoint = 2,

    /// <summary>
    /// Partial cut leaving three points uncut.
    /// </summary>
    PartialThreePoint = 3,
}

/// <summary>
/// Type of real-time status request (DLE EOT n parameter).
/// </summary>
public enum StatusRequestType : byte
{
    /// <summary>DLE EOT 1 - Printer status</summary>
    PrinterStatus = 0x01,

    /// <summary>DLE EOT 2 - Offline cause status</summary>
    OfflineCause = 0x02,

    /// <summary>DLE EOT 3 - Error cause status</summary>
    ErrorCause = 0x03,

    /// <summary>DLE EOT 4 - Paper roll sensor status</summary>
    PaperRollSensor = 0x04
}
