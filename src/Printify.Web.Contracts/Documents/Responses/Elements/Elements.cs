using System.Text.Json.Serialization;
using Printify.Web.Contracts.Documents.Shared.Elements;

namespace Printify.Web.Contracts.Documents.Responses.Elements;

/// <summary>
/// Base contract for response payload elements; used for polymorphic JSON binding.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Bell), "bell")]
[JsonDerivedType(typeof(Error), "error")]
[JsonDerivedType(typeof(Pagecut), "pagecut")]
[JsonDerivedType(typeof(PrinterError), "printerError")]
[JsonDerivedType(typeof(PrinterStatus), "printerStatus")]
[JsonDerivedType(typeof(PrintBarcode), "printBarcode")]
[JsonDerivedType(typeof(PrintQrCode), "printQrCode")]
[JsonDerivedType(typeof(Pulse), "pulse")]
[JsonDerivedType(typeof(RasterImageDto), "rasterImage")]
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
public abstract record ResponseElement : BaseElement;

/// <summary>
/// Marker base for printing response elements that render visible output.
/// </summary>
public abstract record ResponsePrintingElement : ResponseElement;

/// <summary>
/// Marker base for response elements that modify state or report diagnostics.
/// </summary>
public abstract record ResponseNonPrintingElement : ResponseElement;

/// <summary>
/// An audible/attention bell signal.
/// </summary>
public sealed record Bell : ResponseNonPrintingElement;

/// <summary>
/// A non-printing error event emitted by the tokenizer/session (e.g., buffer overflow).
/// </summary>
/// <param name="Code">Machine-readable code (e.g., "BufferOverflow", "ParseError").</param>
/// <param name="Message">Human-readable description.</param>
public sealed record Error(string Code, string Message) : ResponseNonPrintingElement;

/// <summary>
/// A paper cut operation (full or partial depending on command parsed).
/// </summary>
public sealed record Pagecut : ResponseNonPrintingElement;

/// <summary>
/// Represents a printer-specific error emitted during tokenization (e.g., simulated buffer overflow).
/// </summary>
/// <param name="Message">Human-readable description of the error.</param>
public sealed record PrinterError(string Message) : ResponseNonPrintingElement;

/// <summary>
/// A decoded printer status byte with optional human-readable description.
/// </summary>
/// <param name="StatusByte">Raw status byte value.</param>
/// <param name="Description">Optional decoded description for UI/debugging.</param>
public sealed record PrinterStatus(byte StatusByte, string? Description)
    : ResponseNonPrintingElement;

/// <summary>
/// A cash drawer pulse signal sent to a specific pin.
/// </summary>
/// <param name="Pin">Target drawer pin (e.g., Drawer1/Drawer2).</param>
/// <param name="OnTimeMs">Pulse ON interval in milliseconds.</param>
/// <param name="OffTimeMs">Pulse OFF interval in milliseconds.</param>
public sealed record Pulse(PulsePin Pin, int OnTimeMs, int OffTimeMs)
    : ResponseNonPrintingElement;

/// <summary>
/// Renders a one-dimensional barcode and exposes the stored media descriptor.
/// </summary>
/// <param name="Symbology">Selected barcode symbology.</param>
/// <param name="ContentType">MIME type, e.g. "image/png".</param>
/// <param name="SizeBytes">Size in bytes.</param>
/// <param name="Sha256">Sha256 checksum.</param>
/// <param name="Href">Absolute or app-relative URL to retrieve the media bytes.</param>
public sealed record PrintBarcode(
    BarcodeSymbology Symbology,
    string ContentType,
    long? SizeBytes,
    string? Sha256,
    Uri Href)
    : ResponsePrintingElement;

/// <summary>
/// Emits a QR code render request with a stored media descriptor.
/// </summary>
/// <param name="ContentType">MIME type, e.g. "image/png".</param>
/// <param name="SizeBytes">Size in bytes.</param>
/// <param name="Sha256">Sha256 checksum.</param>
/// <param name="Href">Absolute or app-relative URL to retrieve the media bytes.</param>
public sealed record PrintQrCode(
    string ContentType,
    long? SizeBytes,
    string? Sha256,
    Uri Href)
    : ResponsePrintingElement;

/// <summary>
/// Stores QR code data into the printer memory using GS ( k.
/// </summary>
/// <param name="Content">Payload to be encoded into the QR symbol.</param>
public sealed record StoreQrData(string Content) : ResponseNonPrintingElement;

/// <summary>
/// Descriptor for media stored in external storage.
/// </summary>
/// <param name="ContentType">MIME type, e.g. "image/png".</param>
/// <param name="Length">Length in bytes.</param>
/// <param name="Sha256">Sha256 checksum.</param>
/// <param name="Href">Absolute or app-relative URL to retrieve the media bytes.</param>
public sealed record MediaDto(
    string ContentType,
    long? Length,
    string? Sha256,
    Uri Href);

/// <summary>
/// Descriptor for a raster image stored in external media storage.
/// </summary>
/// <param name="Width">Image width in printer dots.</param>
/// <param name="Height">Image height in printer dots.</param>
/// <param name="Media">Referenced media metadata.</param>
public sealed record RasterImageDto(
    int Width,
    int Height,
    MediaDto Media)
    : ResponsePrintingElement;

/// <summary>
/// A printable line of text emitted by the printer protocol.
/// </summary>
/// <param name="Text">Raw text content (decoded as parsed; typically ASCII/CP437 in MVP).</param>
public sealed record TextLine(string Text) : ResponsePrintingElement;

/// <summary>
/// Prints a logo stored in printer memory by its identifier via ESC/POS stored logo commands (e.g., FS p).
/// </summary>
/// <param name="LogoId">Identifier/index of the stored logo in printer memory.</param>
public sealed record StoredLogo(int LogoId) : ResponsePrintingElement;

/// <summary>
/// Resets the printer to its power-on state (ESC @).
/// </summary>
public sealed record ResetPrinter : ResponseNonPrintingElement;

/// <summary>
/// Configures the height of subsequent barcodes using GS h (0x1D 0x68).
/// </summary>
/// <param name="HeightInDots">Barcode height in dots.</param>
public sealed record SetBarcodeHeight(int HeightInDots) : ResponseNonPrintingElement;

/// <summary>
/// Selects the placement of human-readable barcode labels via GS H (0x1D 0x48).
/// </summary>
/// <param name="Position">Desired label positioning.</param>
public sealed record SetBarcodeLabelPosition(BarcodeLabelPosition Position)
    : ResponseNonPrintingElement;

/// <summary>
/// Configures the module width (basic bar width) for subsequent barcodes using GS w (0x1D 0x77).
/// </summary>
/// <param name="ModuleWidth">Module width in device units (typically dots).</param>
public sealed record SetBarcodeModuleWidth(int ModuleWidth)
    : ResponseNonPrintingElement;

/// <summary>
/// Enables or disables emphasized (bold) text mode (ESC E).
/// </summary>
/// <param name="IsEnabled">True when bold mode is turned on; false when turned off.</param>
public sealed record SetBoldMode(bool IsEnabled) : ResponseNonPrintingElement;

/// <summary>
/// Sets the code page used to decode incoming bytes to text.
/// </summary>
/// <param name="CodePage">Code page identifier/name (e.g., "CP437", "CP850").</param>
public sealed record SetCodePage(string CodePage) : ResponseNonPrintingElement;

/// <summary>
/// Changes the active font selection for subsequent printed text using ESC ! (0x1B 0x21) semantics.
/// </summary>
/// <param name="FontNumber">Protocol-specific font number (e.g., 0=A, 1=B).</param>
/// <param name="IsDoubleWidth">True when double-width bit is set.</param>
/// <param name="IsDoubleHeight">True when double-height bit is set.</param>
public sealed record SetFont(int FontNumber, bool IsDoubleWidth, bool IsDoubleHeight)
    : ResponseNonPrintingElement;

/// <summary>
/// Selects justification for subsequent printable data using ESC a (0x1B 0x61).
/// </summary>
/// <param name="Justification">Requested alignment value.</param>
public sealed record SetJustification(TextJustification Justification)
    : ResponseNonPrintingElement;

/// <summary>
/// Sets line spacing in printer dots for subsequent lines (e.g., ESC 0x1B 0x33 n).
/// </summary>
/// <param name="Spacing">Line spacing value in dots.</param>
public sealed record SetLineSpacing(int Spacing) : ResponseNonPrintingElement;

/// <summary>
/// Resets the line spacing to the device default value.
/// </summary>
public sealed record ResetLineSpacing : ResponseNonPrintingElement;

/// <summary>
/// Selects the QR error correction level for subsequent symbols via GS ( k.
/// </summary>
/// <param name="Level">Chosen QR error correction level.</param>
public sealed record SetQrErrorCorrection(QrErrorCorrectionLevel Level)
    : ResponseNonPrintingElement;

/// <summary>
/// Configures the QR code model for subsequent GS ( k sequences.
/// </summary>
/// <param name="Model">Selected QR code model.</param>
public sealed record SetQrModel(QrModel Model) : ResponseNonPrintingElement;

/// <summary>
/// Sets the module size (dot width) for QR codes via GS ( k.
/// </summary>
/// <param name="ModuleSize">Width of a single QR module in dots.</param>
public sealed record SetQrModuleSize(int ModuleSize)
    : ResponseNonPrintingElement;

/// <summary>
/// Enables or disables reverse (white-on-black) print mode (GS B).
/// </summary>
/// <param name="IsEnabled">True when reverse mode is turned on; false when turned off.</param>
public sealed record SetReverseMode(bool IsEnabled) : ResponseNonPrintingElement;

/// <summary>
/// Enables or disables underline text mode (ESC -).
/// </summary>
/// <param name="IsEnabled">True when underline mode is turned on; false when turned off.</param>
public sealed record SetUnderlineMode(bool IsEnabled) : ResponseNonPrintingElement;
