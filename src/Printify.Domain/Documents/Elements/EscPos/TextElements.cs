using Printify.Domain.Media;
using DomainMedia = Printify.Domain.Media.Media;

namespace Printify.Domain.Documents.Elements.EscPos;

/// <summary>
/// Text bytes that are appended to the line buffer.
/// </summary>
/// <param name="Text">Raw text content (decoded as parsed; typically ASCII/CP437 in MVP).</param>
public sealed record AppendText(string Text) : PrintingElement;

/// <summary>
/// Flushes the current line buffer and feeds one line.
/// </summary>
public sealed record PrintAndLineFeed : PrintingElement;

/// <summary>
/// Legacy carriage return kept for compatibility; ignored by the printer.
/// </summary>
public sealed record LegacyCarriageReturn : NonPrintingElement;

/// <summary>
/// Changes the active font selection for subsequent printed text using ESC ! (0x1B 0x21) semantics.
/// </summary>
/// <param name="FontNumber">Protocol-specific font number (e.g., 0=A, 1=B).</param>
/// <param name="IsDoubleWidth">True when double-width bit is set.</param>
/// <param name="IsDoubleHeight">True when double-height bit is set.</param>
public sealed record SelectFont(int FontNumber, bool IsDoubleWidth, bool IsDoubleHeight)
    : NonPrintingElement;

/// <summary>
/// Enables or disables emphasized (bold) text mode (ESC E).
/// </summary>
/// <param name="IsEnabled">True when bold mode is turned on; false when turned off.</param>
public sealed record SetBoldMode(bool IsEnabled) : NonPrintingElement;

/// <summary>
/// Enables or disables underline text mode (ESC -).
/// </summary>
/// <param name="IsEnabled">True when underline mode is turned on; false when turned off.</param>
public sealed record SetUnderlineMode(bool IsEnabled) : NonPrintingElement;

/// <summary>
/// Enables or disables reverse (white-on-black) print mode (GS B).
/// </summary>
/// <param name="IsEnabled">True when reverse mode is turned on; false when turned off.</param>
public sealed record SetReverseMode(bool IsEnabled) : NonPrintingElement;

/// <summary>
/// Selects justification for subsequent printable data using ESC a (0x1B 0x61).
/// </summary>
/// <param name="Justification">Requested alignment value.</param>
public sealed record SetJustification(TextJustification Justification)
    : NonPrintingElement;

/// <summary>
/// Sets line spacing in printer dots for subsequent lines (e.g., ESC 0x1B 0x33 n).
/// </summary>
/// <param name="Spacing">Line spacing value in dots.</param>
public sealed record SetLineSpacing(int Spacing) : NonPrintingElement;

/// <summary>
/// Resets the line spacing to the printer default value.
/// </summary>
public sealed record ResetLineSpacing : NonPrintingElement;

/// <summary>
/// Sets the code page used to decode incoming bytes to text.
/// </summary>
/// <param name="CodePage">Code page identifier/name (e.g., "CP437", "CP850").</param>
public sealed record SetCodePage(string CodePage) : NonPrintingElement;

/// <summary>
/// Prints a logo stored in printer memory by its identifier.
/// Corresponds to ESC/POS stored logo commands (e.g., FS p).
/// </summary>
/// <param name="LogoId">Identifier/index of the stored logo in printer memory.</param>
public sealed record StoredLogo(int LogoId) : PrintingElement;

/// <summary>
/// Raster image that carries the media payload directly in the element.
/// </summary>
/// <param name="Width">Image width in printer dots.</param>
/// <param name="Height">Image height in printer dots.</param>
/// <param name="Media">Raster image media payload, including raw bytes and associated metadata.</param>
public sealed record RasterImageUpload(
    int Width,
    int Height,
    MediaUpload Media)
    : BaseRasterImage(Width, Height);

/// <summary>
/// Raster image that references persisted media content.
/// </summary>
/// <param name="Width">Image width in printer dots.</param>
/// <param name="Height">Image height in printer dots.</param>
/// <param name="Media">Persisted media descriptor with accessible URL.</param>
public sealed record RasterImage(
    int Width,
    int Height,
    DomainMedia Media)
    : BaseRasterImage(Width, Height);

/// <summary>
/// Base type for raster images with shared geometry across content or descriptors.
/// </summary>
/// <param name="Width">Image width in printer dots.</param>
/// <param name="Height">Image height in printer dots.</param>
public abstract record BaseRasterImage(
    int Width,
    int Height)
    : PrintingElement;
