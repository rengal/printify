using System.Text.Json.Serialization;
using Printify.Web.Contracts.Documents.Shared.Elements;

namespace Printify.Web.Contracts.Documents.Requests.Elements;

/// <summary>
/// Base contract for request payload elements; used for polymorphic JSON binding.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Bell), "bell")]
[JsonDerivedType(typeof(Error), "error")]
[JsonDerivedType(typeof(Pagecut), "pagecut")]
[JsonDerivedType(typeof(PrinterError), "printerError")]
[JsonDerivedType(typeof(PrinterStatus), "printerStatus")]
[JsonDerivedType(typeof(PrintBarcode), "printBarcode")]
[JsonDerivedType(typeof(PrintQrCode), "printQrCode")]
[JsonDerivedType(typeof(Pulse), "pulse")]
[JsonDerivedType(typeof(RasterImageContent), "rasterImageContent")]
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
public abstract record RequestElement(int Sequence) : BaseElement(Sequence);

/// <summary>
/// Marker base for printing request elements that render visible output.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public abstract record RequestPrintingElement(int Sequence) : PrintingElement(Sequence);

/// <summary>
/// Marker base for request elements that modify state or report diagnostics.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public abstract record RequestNonPrintingElement(int Sequence) : NonPrintingElement(Sequence);

/// <summary>
/// An audible/attention bell signal.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public sealed record Bell(int Sequence) : RequestNonPrintingElement(Sequence);

/// <summary>
/// A non-printing error event emitted by the tokenizer/session (e.g., buffer overflow).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Code">Machine-readable code (e.g., "BufferOverflow", "ParseError").</param>
/// <param name="Message">Human-readable description.</param>
public sealed record Error(int Sequence, string Code, string Message) : RequestNonPrintingElement(Sequence);

/// <summary>
/// A paper cut operation (full or partial depending on command parsed).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public sealed record Pagecut(int Sequence) : RequestNonPrintingElement(Sequence);

/// <summary>
/// Represents a printer-specific error emitted during tokenization (e.g., simulated buffer overflow).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Message">Human-readable description of the error.</param>
public sealed record PrinterError(int Sequence, string Message) : RequestNonPrintingElement(Sequence);

/// <summary>
/// A decoded printer status byte with optional human-readable description.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="StatusByte">Raw status byte value.</param>
/// <param name="Description">Optional decoded description for UI/debugging.</param>
public sealed record PrinterStatus(int Sequence, byte StatusByte, string? Description)
    : RequestNonPrintingElement(Sequence);

/// <summary>
/// A cash drawer pulse signal sent to a specific pin.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Pin">Target drawer pin (e.g., Drawer1/Drawer2).</param>
/// <param name="OnTimeMs">Pulse ON interval in milliseconds.</param>
/// <param name="OffTimeMs">Pulse OFF interval in milliseconds.</param>
public sealed record Pulse(int Sequence, PulsePin Pin, int OnTimeMs, int OffTimeMs)
    : RequestNonPrintingElement(Sequence);

/// <summary>
/// Renders a one-dimensional barcode using the GS k command family.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Symbology">Selected barcode symbology.</param>
/// <param name="Data">Raw data payload to encode.</param>
public sealed record PrintBarcode(int Sequence, BarcodeSymbology Symbology, string Data)
    : RequestPrintingElement(Sequence);

/// <summary>
/// Emits a QR code print request using the last stored payload.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Content">Payload rendered by the QR symbol.</param>
public sealed record PrintQrCode(int Sequence, string Content) : RequestPrintingElement(Sequence);

/// <summary>
/// Stores QR code data into the printer memory using GS ( k.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Content">Payload to be encoded into the QR symbol.</param>
public sealed record StoreQrData(int Sequence, string Content) : RequestNonPrintingElement(Sequence);

/// <summary>
/// Base type for raster images with shared geometry captured in the request.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Width">Image width in printer dots.</param>
/// <param name="Height">Image height in printer dots.</param>
/// <param name="ContentType">MIME type, e.g. "image/png".</param>
/// <param name="SizeBytes">Size in bytes.</param>
/// <param name="Sha256">Sha256 checksum.</param>
/// <param name="Content">Optional buffer containing the media content.</param>
public sealed record RasterImageContent(
    int Sequence,
    int Width,
    int Height,
    string ContentType,
    long? SizeBytes,
    string? Sha256,
    ReadOnlyMemory<byte>? Content)
    : RequestPrintingElement(Sequence);

/// <summary>
/// A printable line of text emitted by the printer protocol.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Text">Raw text content (decoded as parsed; typically ASCII/CP437 in MVP).</param>
public sealed record TextLine(int Sequence, string Text) : RequestPrintingElement(Sequence);

/// <summary>
/// Prints a logo stored in printer memory by its identifier via ESC/POS stored logo commands (e.g., FS p).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="LogoId">Identifier/index of the stored logo in printer memory.</param>
public sealed record StoredLogo(int Sequence, int LogoId) : RequestPrintingElement(Sequence);

/// <summary>
/// Resets the printer to its power-on state (ESC @).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public sealed record ResetPrinter(int Sequence) : RequestNonPrintingElement(Sequence);

/// <summary>
/// Configures the height of subsequent barcodes using GS h (0x1D 0x68).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="HeightInDots">Barcode height in dots.</param>
public sealed record SetBarcodeHeight(int Sequence, int HeightInDots) : RequestNonPrintingElement(Sequence);

/// <summary>
/// Selects the placement of human-readable barcode labels via GS H (0x1D 0x48).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Position">Desired label positioning.</param>
public sealed record SetBarcodeLabelPosition(int Sequence, BarcodeLabelPosition Position)
    : RequestNonPrintingElement(Sequence);

/// <summary>
/// Configures the module width (basic bar width) for subsequent barcodes using GS w (0x1D 0x77).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="ModuleWidth">Module width in device units (typically dots).</param>
public sealed record SetBarcodeModuleWidth(int Sequence, int ModuleWidth)
    : RequestNonPrintingElement(Sequence);

/// <summary>
/// Enables or disables emphasized (bold) text mode (ESC E).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="IsEnabled">True when bold mode is turned on; false when turned off.</param>
public sealed record SetBoldMode(int Sequence, bool IsEnabled) : RequestNonPrintingElement(Sequence);

/// <summary>
/// Sets the code page used to decode incoming bytes to text.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="CodePage">Code page identifier/name (e.g., "CP437", "CP850").</param>
public sealed record SetCodePage(int Sequence, string CodePage) : RequestNonPrintingElement(Sequence);

/// <summary>
/// Changes the active font selection for subsequent printed text using ESC ! (0x1B 0x21) semantics.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="FontNumber">Protocol-specific font number (e.g., 0=A, 1=B).</param>
/// <param name="IsDoubleWidth">True when double-width bit is set.</param>
/// <param name="IsDoubleHeight">True when double-height bit is set.</param>
public sealed record SetFont(int Sequence, int FontNumber, bool IsDoubleWidth, bool IsDoubleHeight)
    : RequestNonPrintingElement(Sequence);

/// <summary>
/// Selects justification for subsequent printable data using ESC a (0x1B 0x61).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Justification">Requested alignment value.</param>
public sealed record SetJustification(int Sequence, TextJustification Justification)
    : RequestNonPrintingElement(Sequence);

/// <summary>
/// Sets line spacing in printer dots for subsequent lines (e.g., ESC 0x1B 0x33 n).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Spacing">Line spacing value in dots.</param>
public sealed record SetLineSpacing(int Sequence, int Spacing) : RequestNonPrintingElement(Sequence);

/// <summary>
/// Resets the line spacing to the device default value.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public sealed record ResetLineSpacing(int Sequence) : RequestNonPrintingElement(Sequence);

/// <summary>
/// Selects the QR error correction level for subsequent symbols via GS ( k.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Level">Chosen QR error correction level.</param>
public sealed record SetQrErrorCorrection(int Sequence, QrErrorCorrectionLevel Level)
    : RequestNonPrintingElement(Sequence);

/// <summary>
/// Configures the QR code model for subsequent GS ( k sequences.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Model">Selected QR code model.</param>
public sealed record SetQrModel(int Sequence, QrModel Model) : RequestNonPrintingElement(Sequence);

/// <summary>
/// Sets the module size (dot width) for QR codes via GS ( k.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="ModuleSize">Width of a single QR module in dots.</param>
public sealed record SetQrModuleSize(int Sequence, int ModuleSize)
    : RequestNonPrintingElement(Sequence);

/// <summary>
/// Enables or disables reverse (white-on-black) print mode (GS B).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="IsEnabled">True when reverse mode is turned on; false when turned off.</param>
public sealed record SetReverseMode(int Sequence, bool IsEnabled) : RequestNonPrintingElement(Sequence);

/// <summary>
/// Enables or disables underline text mode (ESC -).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="IsEnabled">True when underline mode is turned on; false when turned off.</param>
public sealed record SetUnderlineMode(int Sequence, bool IsEnabled) : RequestNonPrintingElement(Sequence);
