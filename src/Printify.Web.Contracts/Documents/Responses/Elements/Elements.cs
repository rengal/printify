using Printify.Web.Contracts.Documents.Shared.Elements;
using System.Text.Json.Serialization;

namespace Printify.Web.Contracts.Documents.Responses.Elements;

/// <summary>
/// Base contract for response payload elements; used for polymorphic JSON binding.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Bell), "bell")]
[JsonDerivedType(typeof(ErrorDto), "error")]
[JsonDerivedType(typeof(PagecutDto), "pagecut")]
[JsonDerivedType(typeof(PrinterStatusDto), "printerStatus")]
[JsonDerivedType(typeof(PrintBarcodeDto), "printBarcode")]
[JsonDerivedType(typeof(PrintQrCodeDto), "printQrCode")]
[JsonDerivedType(typeof(PulseDto), "pulse")]
[JsonDerivedType(typeof(RasterImageDto), "rasterImage")]
[JsonDerivedType(typeof(ResetPrinterDto), "resetPrinter")]
[JsonDerivedType(typeof(SetBarcodeHeightDto), "setBarcodeHeight")]
[JsonDerivedType(typeof(SetBarcodeLabelPositionDto), "setBarcodeLabelPosition")]
[JsonDerivedType(typeof(SetBarcodeModuleWidthDto), "setBarcodeModuleWidth")]
[JsonDerivedType(typeof(SetBoldModeDto), "setBoldMode")]
[JsonDerivedType(typeof(SetCodePageDto), "setCodePage")]
[JsonDerivedType(typeof(SetFontDto), "setFont")]
[JsonDerivedType(typeof(SetJustificationDto), "setJustification")]
[JsonDerivedType(typeof(SetLineSpacingDto), "setLineSpacing")]
[JsonDerivedType(typeof(ResetLineSpacingDto), "resetLineSpacing")]
[JsonDerivedType(typeof(SetQrErrorCorrectionDto), "setQrErrorCorrection")]
[JsonDerivedType(typeof(SetQrModelDto), "setQrModel")]
[JsonDerivedType(typeof(SetQrModuleSizeDto), "setQrModuleSize")]
[JsonDerivedType(typeof(SetReverseModeDto), "setReverseMode")]
[JsonDerivedType(typeof(SetUnderlineModeDto), "setUnderlineMode")]
[JsonDerivedType(typeof(StoreQrDataDto), "storeQrData")]
[JsonDerivedType(typeof(StoredLogoDto), "storedLogo")]
[JsonDerivedType(typeof(AppendToLineBufferDto), "appendToLineBuffer")]
[JsonDerivedType(typeof(FlushLineBufferAndFeedDto), "flushLineBufferAndFeed")]
[JsonDerivedType(typeof(LegacyCarriageReturnDto), "legacyCarriageReturn")]
public abstract record ResponseElementDto : BaseElementDto;

/// <summary>
/// Marker base for printing response elements that render visible output.
/// </summary>
public abstract record ResponsePrintingElementDto : ResponseElementDto;

/// <summary>
/// Marker base for response elements that modify state or report diagnostics.
/// </summary>
public abstract record ResponseNonPrintingElementDto : ResponseElementDto;

/// <summary>
/// An audible/attention bell signal.
/// </summary>
public sealed record Bell : ResponseNonPrintingElementDto;

/// <summary>
/// A non-printing error event emitted by the tokenizer/session (e.g., buffer overflow).
/// </summary>
/// <param name="Code">Machine-readable code (e.g., "BufferOverflow", "ParseError").</param>
/// <param name="Message">Human-readable description.</param>
public sealed record ErrorDto(string Code, string Message) : ResponseNonPrintingElementDto;

/// <summary>
/// A paper cut operation (full or partial depending on command parsed).
/// </summary>
public sealed record PagecutDto : ResponseNonPrintingElementDto;

/// <summary>
/// A decoded printer status byte with optional human-readable description.
/// </summary>
/// <param name="StatusByte">Raw status byte value.</param>
/// <param name="Description">Optional decoded description for UI/debugging.</param>
public sealed record PrinterStatusDto(byte StatusByte, string? Description)
    : ResponseNonPrintingElementDto;

/// <summary>
/// A cash drawer pulse signal sent to a specific pin.
/// </summary>
/// <param name="Pin">Target drawer pin.</param>
/// <param name="OnTimeMs">Pulse ON interval in milliseconds.</param>
/// <param name="OffTimeMs">Pulse OFF interval in milliseconds.</param>
public sealed record PulseDto(int Pin, int OnTimeMs, int OffTimeMs)
    : ResponseNonPrintingElementDto;

/// <summary>
/// Descriptor for media stored in external storage.
/// </summary>
/// <param name="ContentType">MIME type, e.g. "image/png".</param>
/// <param name="Length">Length in bytes.</param>
/// <param name="Sha256">Sha256 checksum.</param>
/// <param name="Url">Relative URL to retrieve the media bytes.</param>
public sealed record MediaDto(
    string ContentType,
    long Length,
    string Sha256,
    string Url);

/// <summary>
/// Renders a one-dimensional barcode and exposes the stored media descriptor.
/// </summary>
/// <param name="Symbology">Selected barcode symbology.</param>
/// <param name="Width">Image width in printer dots.</param>
/// <param name="Height">Image height in printer dots.</param>
/// <param name="Media">Referenced media metadata.</param>
public sealed record PrintBarcodeDto(
    string Symbology,
    int Width,
    int Height,
    MediaDto Media)
    : ResponsePrintingElementDto;

/// <summary>
/// Emits a QR code render request with a stored media descriptor.
/// </summary>
/// <param name="Width">Image width in printer dots.</param>
/// <param name="Height">Image height in printer dots.</param>
/// <param name="Media">Referenced media metadata.</param>
public sealed record PrintQrCodeDto(
    string Data,
    int Width,
    int Height,
    MediaDto Media)
    : ResponsePrintingElementDto;

/// <summary>
/// Stores QR code data into the printer memory using GS ( k.
/// </summary>
/// <param name="Content">Payload to be encoded into the QR symbol.</param>
public sealed record StoreQrDataDto(string Content) : ResponseNonPrintingElementDto;

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
    : ResponsePrintingElementDto;

/// <summary>
/// A printable line of text emitted by the printer protocol.
/// </summary>
/// <param name="Text">Raw text content (decoded as parsed; typically ASCII/CP437 in MVP).</param>
public sealed record AppendToLineBufferDto(string Text) : ResponsePrintingElementDto;

/// <summary>
/// Flushes the line buffer and feeds one line.
/// </summary>
public sealed record FlushLineBufferAndFeedDto : ResponsePrintingElementDto;

/// <summary>
/// Legacy carriage return kept for compatibility; ignored by the printer.
/// </summary>
public sealed record LegacyCarriageReturnDto : ResponseNonPrintingElementDto;

/// <summary>
/// Prints a logo stored in printer memory by its identifier via ESC/POS stored logo commands (e.g., FS p).
/// </summary>
/// <param name="LogoId">Identifier/index of the stored logo in printer memory.</param>
public sealed record StoredLogoDto(int LogoId) : ResponsePrintingElementDto;

/// <summary>
/// Resets the printer to its power-on state (ESC @).
/// </summary>
public sealed record ResetPrinterDto : ResponseNonPrintingElementDto;

/// <summary>
/// Configures the height of subsequent barcodes using GS h (0x1D 0x68).
/// </summary>
/// <param name="HeightInDots">Barcode height in dots.</param>
public sealed record SetBarcodeHeightDto(int HeightInDots) : ResponseNonPrintingElementDto;

/// <summary>
/// Selects the placement of human-readable barcode labels via GS H (0x1D 0x48).
/// </summary>
/// <param name="Position">Desired label positioning.</param>
public sealed record SetBarcodeLabelPositionDto(string Position)
    : ResponseNonPrintingElementDto;

/// <summary>
/// Configures the module width (basic bar width) for subsequent barcodes using GS w (0x1D 0x77).
/// </summary>
/// <param name="ModuleWidth">Module width in device units (typically dots).</param>
public sealed record SetBarcodeModuleWidthDto(int ModuleWidth)
    : ResponseNonPrintingElementDto;

/// <summary>
/// Enables or disables emphasized (bold) text mode (ESC E).
/// </summary>
/// <param name="IsEnabled">True when bold mode is turned on; false when turned off.</param>
public sealed record SetBoldModeDto(bool IsEnabled) : ResponseNonPrintingElementDto;

/// <summary>
/// Sets the code page used to decode incoming bytes to text.
/// </summary>
/// <param name="CodePage">Code page identifier/name (e.g., "CP437", "CP850").</param>
public sealed record SetCodePageDto(string CodePage) : ResponseNonPrintingElementDto;

/// <summary>
/// Changes the active font selection for subsequent printed text using ESC ! (0x1B 0x21) semantics.
/// </summary>
/// <param name="FontNumber">Protocol-specific font number (e.g., 0=A, 1=B).</param>
/// <param name="IsDoubleWidth">True when double-width bit is set.</param>
/// <param name="IsDoubleHeight">True when double-height bit is set.</param>
public sealed record SetFontDto(int FontNumber, bool IsDoubleWidth, bool IsDoubleHeight)
    : ResponseNonPrintingElementDto;

/// <summary>
/// Selects justification for subsequent printable data using ESC a (0x1B 0x61).
/// </summary>
/// <param name="Justification">Requested alignment value.</param>
public sealed record SetJustificationDto(string Justification)
    : ResponseNonPrintingElementDto;

/// <summary>
/// Sets line spacing in printer dots for subsequent lines (e.g., ESC 0x1B 0x33 n).
/// </summary>
/// <param name="Spacing">Line spacing value in dots.</param>
public sealed record SetLineSpacingDto(int Spacing) : ResponseNonPrintingElementDto;

/// <summary>
/// Resets the line spacing to the device default value.
/// </summary>
public sealed record ResetLineSpacingDto : ResponseNonPrintingElementDto;

/// <summary>
/// Selects the QR error correction level for subsequent symbols via GS ( k.
/// </summary>
/// <param name="Level">Chosen QR error correction level.</param>
public sealed record SetQrErrorCorrectionDto(string Level)
    : ResponseNonPrintingElementDto;

/// <summary>
/// Configures the QR code model for subsequent GS ( k sequences.
/// </summary>
/// <param name="Model">Selected QR code model.</param>
public sealed record SetQrModelDto(string Model) : ResponseNonPrintingElementDto;

/// <summary>
/// Sets the module size (dot width) for QR codes via GS ( k.
/// </summary>
/// <param name="ModuleSize">Width of a single QR module in dots.</param>
public sealed record SetQrModuleSizeDto(int ModuleSize)
    : ResponseNonPrintingElementDto;

/// <summary>
/// Enables or disables reverse (white-on-black) print mode (GS B).
/// </summary>
/// <param name="IsEnabled">True when reverse mode is turned on; false when turned off.</param>
public sealed record SetReverseModeDto(bool IsEnabled) : ResponseNonPrintingElementDto;

/// <summary>
/// Enables or disables underline text mode (ESC -).
/// </summary>
/// <param name="IsEnabled">True when underline mode is turned on; false when turned off.</param>
public sealed record SetUnderlineModeDto(bool IsEnabled) : ResponseNonPrintingElementDto;
