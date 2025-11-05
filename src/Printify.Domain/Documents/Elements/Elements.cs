using System.Text.Json.Serialization;

using Printify.Domain.Media;

namespace Printify.Domain.Documents.Elements;

/// <summary>
/// Base type for all document elements produced by tokenizers and consumed by renderers.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "")]
[JsonDerivedType(typeof(Bell), "bell")]
[JsonDerivedType(typeof(Error), "error")]
[JsonDerivedType(typeof(Pagecut), "pagecut")]
[JsonDerivedType(typeof(PrinterError), "printerError")]
[JsonDerivedType(typeof(PrinterStatus), "printerStatus")]
[JsonDerivedType(typeof(PrintBarcode), "printBarcode")]
[JsonDerivedType(typeof(PrintQrCode), "printQrCode")]
[JsonDerivedType(typeof(Pulse), "pulse")]
[JsonDerivedType(typeof(RasterImageContent), "rasterImageContent")]
[JsonDerivedType(typeof(RasterImageDescriptor), "rasterImageDescriptor")]
[JsonDerivedType(typeof(ResetPrinter), "resetPrinter")]
[JsonDerivedType(typeof(SetBarcodeHeight), "setBarcodeHeight")]
[JsonDerivedType(typeof(SetBarcodeLabelPosition), "setBarcodeLabelPosition")]
[JsonDerivedType(typeof(SetBarcodeModuleWidth), "setBarcodeModuleWidth")]
[JsonDerivedType(typeof(SetBoldMode), "setBoldMode")]
[JsonDerivedType(typeof(SetCodePage), "setCodePage")]
[JsonDerivedType(typeof(SetFont), "setFont")]
[JsonDerivedType(typeof(SetJustification), "setJustification")]
[JsonDerivedType(typeof(SetLineSpacing), "setLineSpacing")]
[JsonDerivedType(typeof(ResetLineSpacing), "resetLineSpacing")]
[JsonDerivedType(typeof(SetQrErrorCorrection), "setQrErrorCorrection")]
[JsonDerivedType(typeof(SetQrModel), "setQrModel")]
[JsonDerivedType(typeof(SetQrModuleSize), "setQrModuleSize")]
[JsonDerivedType(typeof(SetReverseMode), "setReverseMode")]
[JsonDerivedType(typeof(SetUnderlineMode), "setUnderlineMode")]
[JsonDerivedType(typeof(StoreQrData), "storeQrData")]
[JsonDerivedType(typeof(StoredLogo), "storedLogo")]
[JsonDerivedType(typeof(TextLine), "textLine")]
public abstract record Element(int Sequence);

/// <summary>
/// Base type for non-printing control or status events within a document stream.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public abstract record NonPrintingElement(int Sequence) : Element(Sequence);

/// <summary>
/// Base type for printing (visible) elements that produce output on paper.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public abstract record PrintingElement(int Sequence) : Element(Sequence);

/// <summary>
/// Base type for raster images with shared geometry across content or descriptors.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Width">Image width in printer dots.</param>
/// <param name="Height">Image height in printer dots.</param>
public abstract record BaseRasterImage(
    int Sequence,
    int Width,
    int Height)
    : PrintingElement(Sequence);

/// <summary>
/// Raster image that carries the media payload directly in the element.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Width">Image width in printer dots.</param>
/// <param name="Height">Image height in printer dots.</param>
/// <param name="Media">Media payload (bytes plus metadata) for the raster image.</param>
public sealed record RasterImageContent(
    int Sequence,
    int Width,
    int Height,
    MediaContent Media)
    : BaseRasterImage(Sequence, Width, Height);

/// <summary>
/// Raster image that references media via a descriptor (URL plus metadata).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Width">Image width in printer dots.</param>
/// <param name="Height">Image height in printer dots.</param>
/// <param name="Media">Descriptor describing where the raster image bytes can be retrieved.</param>
public sealed record RasterImageDescriptor(
    int Sequence,
    int Width,
    int Height,
    MediaDescriptor Media)
    : BaseRasterImage(Sequence, Width, Height);

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
/// Cash drawer pin selection for pulse commands.
/// </summary>
public enum PulsePin
{
    /// <summary>Drawer 1 pin.</summary>
    Drawer1,

    /// <summary>Drawer 2 pin.</summary>
    Drawer2
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
/// An audible/attention bell signal.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public sealed record Bell(int Sequence) : NonPrintingElement(Sequence);

/// <summary>
/// A non-printing error event emitted by the tokenizer/session (e.g., buffer overflow).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Code">Machine-readable code (e.g., "BufferOverflow", "ParseError").</param>
/// <param name="Message">Human-readable description.</param>
public sealed record Error(int Sequence, string Code, string Message) : NonPrintingElement(Sequence);

/// <summary>
/// A paper cut operation (full or partial depending on command parsed).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public sealed record Pagecut(int Sequence) : NonPrintingElement(Sequence);

/// <summary>
/// Represents a printer-specific error emitted during tokenization (e.g., simulated buffer overflow).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Message">Human-readable description of the error.</param>
public sealed record PrinterError(int Sequence, string Message) : NonPrintingElement(Sequence);

/// <summary>
/// A decoded printer status byte with optional human-readable description.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="StatusByte">Raw status byte value.</param>
/// <param name="Description">Optional decoded description for UI/debugging.</param>
public sealed record PrinterStatus(int Sequence, byte StatusByte, string? Description)
    : NonPrintingElement(Sequence);

/// <summary>
/// Renders a one-dimensional barcode using the GS k command family.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Symbology">Selected barcode symbology.</param>
/// <param name="Data">Raw data payload to encode.</param>
public sealed record PrintBarcode(int Sequence, BarcodeSymbology Symbology, string Data)
    : PrintingElement(Sequence);

/// <summary>
/// Emits a QR code print request using the last stored payload.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Content">Payload rendered by the QR symbol.</param>
public sealed record PrintQrCode(int Sequence, string Content) : PrintingElement(Sequence);

/// <summary>
/// A cash drawer pulse signal sent to a specific pin.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Pin">Target drawer pin (e.g., Drawer1/Drawer2).</param>
/// <param name="OnTimeMs">Pulse ON interval in milliseconds.</param>
/// <param name="OffTimeMs">Pulse OFF interval in milliseconds.</param>
public sealed record Pulse(int Sequence, PulsePin Pin, int OnTimeMs, int OffTimeMs)
    : NonPrintingElement(Sequence);

/// <summary>
/// Resets the printer to its power-on state (ESC @).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public sealed record ResetPrinter(int Sequence) : NonPrintingElement(Sequence);

/// <summary>
/// Configures the height of subsequent barcodes using GS h (0x1D 0x68).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="HeightInDots">Barcode height in dots.</param>
public sealed record SetBarcodeHeight(int Sequence, int HeightInDots) : NonPrintingElement(Sequence);

/// <summary>
/// Selects the placement of human-readable barcode labels via GS H (0x1D 0x48).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Position">Desired label positioning.</param>
public sealed record SetBarcodeLabelPosition(int Sequence, BarcodeLabelPosition Position)
    : NonPrintingElement(Sequence);

/// <summary>
/// Configures the module width (basic bar width) for subsequent barcodes using GS w (0x1D 0x77).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="ModuleWidth">Module width in device units (typically dots).</param>
public sealed record SetBarcodeModuleWidth(int Sequence, int ModuleWidth) : NonPrintingElement(Sequence);

/// <summary>
/// Enables or disables emphasized (bold) text mode (ESC E).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="IsEnabled">True when bold mode is turned on; false when turned off.</param>
public sealed record SetBoldMode(int Sequence, bool IsEnabled) : NonPrintingElement(Sequence);

/// <summary>
/// Sets the code page used to decode incoming bytes to text.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="CodePage">Code page identifier/name (e.g., "CP437", "CP850").</param>
public sealed record SetCodePage(int Sequence, string CodePage) : NonPrintingElement(Sequence);

/// <summary>
/// Changes the active font selection for subsequent printed text using ESC ! (0x1B 0x21) semantics.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="FontNumber">Protocol-specific font number (e.g., 0=A, 1=B).</param>
/// <param name="IsDoubleWidth">True when double-width bit is set.</param>
/// <param name="IsDoubleHeight">True when double-height bit is set.</param>
public sealed record SetFont(int Sequence, int FontNumber, bool IsDoubleWidth, bool IsDoubleHeight)
    : NonPrintingElement(Sequence);

/// <summary>
/// Selects justification for subsequent printable data using ESC a (0x1B 0x61).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Justification">Requested alignment value.</param>
public sealed record SetJustification(int Sequence, TextJustification Justification)
    : NonPrintingElement(Sequence);

/// <summary>
/// Sets line spacing in printer dots for subsequent lines (e.g., ESC 0x1B 0x33 n).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Spacing">Line spacing value in dots.</param>
public sealed record SetLineSpacing(int Sequence, int Spacing) : NonPrintingElement(Sequence);

/// <summary>
/// Resets the line spacing to the printer default value.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public sealed record ResetLineSpacing(int Sequence) : NonPrintingElement(Sequence);

/// <summary>
/// Selects the QR error correction level for subsequent symbols via GS ( k.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Level">Chosen QR error correction level.</param>
public sealed record SetQrErrorCorrection(int Sequence, QrErrorCorrectionLevel Level)
    : NonPrintingElement(Sequence);

/// <summary>
/// Configures the QR code model for subsequent GS ( k sequences.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Model">Selected QR code model.</param>
public sealed record SetQrModel(int Sequence, QrModel Model) : NonPrintingElement(Sequence);

/// <summary>
/// Sets the module size (dot width) for QR codes via GS ( k.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="ModuleSize">Width of a single QR module in dots.</param>
public sealed record SetQrModuleSize(int Sequence, int ModuleSize) : NonPrintingElement(Sequence);

/// <summary>
/// Enables or disables reverse (white-on-black) print mode (GS B).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="IsEnabled">True when reverse mode is turned on; false when turned off.</param>
public sealed record SetReverseMode(int Sequence, bool IsEnabled) : NonPrintingElement(Sequence);

/// <summary>
/// Enables or disables underline text mode (ESC -).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="IsEnabled">True when underline mode is turned on; false when turned off.</param>
public sealed record SetUnderlineMode(int Sequence, bool IsEnabled) : NonPrintingElement(Sequence);

/// <summary>
/// Prints a logo stored in printer memory by its identifier.
/// Corresponds to ESC/POS stored logo commands (e.g., FS p).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="LogoId">Identifier/index of the stored logo in printer memory.</param>
public sealed record StoredLogo(int Sequence, int LogoId) : PrintingElement(Sequence);

/// <summary>
/// Stores QR code data into the printer memory using GS ( k.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Content">Payload to be encoded into the QR symbol.</param>
public sealed record StoreQrData(int Sequence, string Content) : NonPrintingElement(Sequence);

/// <summary>
/// A printable line of text emitted by the printer protocol.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Text">Raw text content (decoded as parsed; typically ASCII/CP437 in MVP).</param>
public sealed record TextLine(int Sequence, string Text) : PrintingElement(Sequence);
