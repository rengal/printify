/*using System.Text.Json.Serialization;
using Printify.Web.Contracts.Documents.Shared.Elements;

namespace Printify.Web.Contracts.Documents.Requests.Elements;

/// <summary>
/// Base contract for request payload elements; used for polymorphic JSON binding.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(BellDto), "bell")]
[JsonDerivedType(typeof(ErrorDto), "error")]
[JsonDerivedType(typeof(PagecutDto), "pagecut")]
[JsonDerivedType(typeof(PrinterErrorDto), "printerError")]
[JsonDerivedType(typeof(PrinterStatusDto), "printerStatus")]
[JsonDerivedType(typeof(PrintBarcodeDto), "printBarcode")]
[JsonDerivedType(typeof(PrintQrCodeDto), "printQrCode")]
[JsonDerivedType(typeof(PulseDto), "pulse")]
[JsonDerivedType(typeof(RasterImageContentDto), "rasterImageContent")]
[JsonDerivedType(typeof(ResetPrinterDto), "resetPrinter")]
[JsonDerivedType(typeof(SetBarcodeHeightDto), "setBarcodeHeight")]
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
[JsonDerivedType(typeof(StoreQrDataDto), "storeQrData")]
[JsonDerivedType(typeof(StoredLogo), "storedLogo")]
[JsonDerivedType(typeof(TextLine), "textLine")]
public abstract record RequestElementDto : BaseElementDto;

/// <summary>
/// Marker base for printing request elements that render visible output.
/// </summary>
public abstract record RequestPrintingElementDto : PrintingElementDto;

/// <summary>
/// Marker base for request elements that modify state or report diagnostics.
/// </summary>
public abstract record RequestNonPrintingElementDto : NonPrintingElementDto;

/// <summary>
/// An audible/attention bell signal.
/// </summary>
public sealed record BellDto : RequestNonPrintingElementDto;

/// <summary>
/// A non-printing error event emitted by the tokenizer/session (e.g., buffer overflow).
/// </summary>
/// <param name="Code">Machine-readable code (e.g., "BufferOverflow", "ParseError").</param>
/// <param name="Message">Human-readable description.</param>
public sealed record ErrorDto(string Code, string Message) : RequestNonPrintingElementDto;

/// <summary>
/// A paper cut operation (full or partial depending on command parsed).
/// </summary>
public sealed record PagecutDto : RequestNonPrintingElementDto;

/// <summary>
/// Represents a printer-specific error emitted during tokenization (e.g., simulated buffer overflow).
/// </summary>
/// <param name="Message">Human-readable description of the error.</param>
public sealed record PrinterErrorDto(string Message) : RequestNonPrintingElementDto;

/// <summary>
/// A decoded printer status byte with optional human-readable description.
/// </summary>
/// <param name="StatusByte">Raw status byte value.</param>
/// <param name="Description">Optional decoded description for UI/debugging.</param>
public sealed record PrinterStatusDto(byte StatusByte, string? Description)
    : RequestNonPrintingElementDto;

/// <summary>
/// A cash drawer pulse signal sent to a specific pin.
/// </summary>
/// <param name="Pin">Target drawer pin number.</param>
/// <param name="OnTimeMs">Pulse ON interval in milliseconds.</param>
/// <param name="OffTimeMs">Pulse OFF interval in milliseconds.</param>
public sealed record PulseDto(int Pin, int OnTimeMs, int OffTimeMs)
    : RequestNonPrintingElementDto;

/// <summary>
/// Printable barcode command payload.
/// </summary>
/// <param name="Symbology">Barcode symbology.</param>
/// <param name="Data">Encoded content.</param>
public sealed record PrintBarcodeDto(string Symbology, string Data)
    : RequestPrintingElementDto;

/// <summary>
/// Printable QR code command payload.
/// </summary>
public sealed record PrintQrCodeDto : RequestPrintingElementDto;

/// <summary>
/// Stores QR payload prior to printing.
/// </summary>
/// <param name="Content">QR payload.</param>
public sealed record StoreQrDataDto(string Content) : RequestNonPrintingElementDto;

/// <summary>
/// Base type for raster images with shared geometry captured in the request.
/// </summary>
/// <param name="Width">Image width in printer dots.</param>
/// <param name="Height">Image height in printer dots.</param>
/// <param name="ContentType">MIME type, e.g. "image/png".</param>
/// <param name="SizeBytes">Size in bytes.</param>
/// <param name="Sha256">Sha256 checksum.</param>
/// <param name="Content">Optional buffer containing the media content.</param>
public sealed record RasterImageContentDto(
    int Width,
    int Height,
    string ContentType,
    long? SizeBytes,
    string? Sha256,
    ReadOnlyMemory<byte>? Content)
    : RequestPrintingElementDto;

/// <summary>
/// A printable line of text emitted by the printer protocol.
/// </summary>
/// <param name="Text">Raw text content.</param>
public sealed record TextLine(string Text) : RequestPrintingElementDto;

/// <summary>
/// Prints a logo stored in printer memory by its identifier via ESC/POS stored logo commands (e.g., FS p).
/// </summary>
/// <param name="LogoId">Identifier/index of the stored logo in printer memory.</param>
public sealed record StoredLogo(int LogoId) : RequestPrintingElementDto;

/// <summary>
/// Resets the printer to its power-on state (ESC @).
/// </summary>
public sealed record ResetPrinterDto : RequestNonPrintingElementDto;

/// <summary>
/// Configures the height of subsequent barcodes using GS h (0x1D 0x68).
/// </summary>
/// <param name="HeightInDots">Barcode height in dots.</param>
public sealed record SetBarcodeHeightDto(int HeightInDots) : RequestNonPrintingElementDto;

/// <summary>
/// Selects the placement of human-readable barcode labels via GS H (0x1D 0x48).
/// </summary>
/// <param name="Position">Desired label positioning.</param>
public sealed record SetBarcodeLabelPosition(string Position)
    : RequestNonPrintingElementDto;

/// <summary>
/// Configures the module width (basic bar width) for subsequent barcodes using GS w (0x1D 0x77).
/// </summary>
/// <param name="ModuleWidth">Module width in device units (typically dots).</param>
public sealed record SetBarcodeModuleWidth(int ModuleWidth)
    : RequestNonPrintingElementDto;

/// <summary>
/// Enables or disables emphasized (bold) text mode (ESC E).
/// </summary>
/// <param name="IsEnabled">True when bold mode is turned on; false when turned off.</param>
public sealed record SetBoldMode(bool IsEnabled) : RequestNonPrintingElementDto;

/// <summary>
/// Sets the code page used to decode incoming bytes to text.
/// </summary>
/// <param name="CodePage">Code page identifier/name (e.g., "CP437", "CP850").</param>
public sealed record SetCodePage(string CodePage) : RequestNonPrintingElementDto;

/// <summary>
/// Changes the active font selection for subsequent printed text using ESC ! (0x1B 0x21) semantics.
/// </summary>
/// <param name="FontNumber">Protocol-specific font number (e.g., 0=A, 1=B).
/// </param>
/// <param name="IsDoubleWidth">True when double-width bit is set.</param>
/// <param name="IsDoubleHeight">True when double-height bit is set.</param>
public sealed record SetFont(int FontNumber, bool IsDoubleWidth, bool IsDoubleHeight)
    : RequestNonPrintingElementDto;

/// <summary>
/// Selects justification for subsequent printable data using ESC a (0x1B 0x61).
/// </summary>
/// <param name="Justification">Requested alignment value.</param>
public sealed record SetJustification(string Justification)
    : RequestNonPrintingElementDto;

/// <summary>
/// Sets line spacing in printer dots for subsequent lines (e.g., ESC 0x1B 0x33 n).
/// </summary>
/// <param name="Spacing">Line spacing value in dots.</param>
public sealed record SetLineSpacing(int Spacing) : RequestNonPrintingElementDto;

/// <summary>
/// Resets the line spacing to the device default value.
/// </summary>
public sealed record ResetLineSpacing : RequestNonPrintingElementDto;

/// <summary>
/// Selects the QR error correction level for subsequent symbols via GS ( k.
/// </summary>
/// <param name="Level">Chosen QR error correction level.</param>
public sealed record SetQrErrorCorrection(string Level)
    : RequestNonPrintingElementDto;

/// <summary>
/// Configures the QR code model for subsequent GS ( k.
/// </summary>
/// <param name="Model">Selected QR code model.</param>
public sealed record SetQrModel(string Model) : RequestNonPrintingElementDto;

/// <summary>
/// Sets the module size (dot width) for QR codes via GS ( k.
/// </summary>
/// <param name="ModuleSize">Width of a single QR module in dots.</param>
public sealed record SetQrModuleSize(int ModuleSize)
    : RequestNonPrintingElementDto;

/// <summary>
/// Enables or disables reverse (white-on-black) print mode (GS B).
/// </summary>
/// <param name="IsEnabled">True when reverse mode is turned on; false when turned off.</param>
public sealed record SetReverseMode(bool IsEnabled) : RequestNonPrintingElementDto;

/// <summary>
/// Enables or disables underline text mode (ESC -).
/// </summary>
/// <param name="IsEnabled">True when underline mode is turned on; false when turned off.</param>
public sealed record SetUnderlineMode(bool IsEnabled) : RequestNonPrintingElementDto;
*/