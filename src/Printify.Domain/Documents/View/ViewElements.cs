using System.Collections.Generic;

namespace Printify.Domain.Documents.View;

/// <summary>
/// Canonical font identifiers for view-oriented rendering.
/// </summary>
public static class ViewFontNames
{
    public const string EscPosA = "ESCPOS_A";
    public const string EscPosB = "ESCPOS_B";

    // EPL scalable fonts (font selection: 2=font 0, 3=font 1, 4=font 2)
    public const string EplFont0 = "EPL_0";
    public const string EplFont1 = "EPL_1";
    public const string EplFont2 = "EPL_2";
}

/// <summary>
/// Base type for view-oriented elements with rendering metadata and debug details.
/// </summary>
public abstract record ViewElement
{
    /// <summary>
    /// Raw command bytes encoded for debugging or UI display.
    /// </summary>
    public string CommandRaw { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable description of the command (one entry per line).
    /// </summary>
    public IReadOnlyList<string> CommandDescription { get; init; } = [];

    /// <summary>
    /// Length of the command in bytes.
    /// </summary>
    public int LengthInBytes { get; init; }

    /// <summary>
    /// Explicit stacking order within the page; higher values render on top.
    /// </summary>
    public int ZIndex { get; init; }
}

/// <summary>
/// Represents a text fragment placed at an absolute position in printer dots.
/// </summary>
/// <param name="Text">Decoded text to render.</param>
/// <param name="X">Left position in dots.</param>
/// <param name="Y">Top position in dots.</param>
/// <param name="Width">Layout width in dots.</param>
/// <param name="Height">Layout height in dots.</param>
/// <param name="Font">Protocol font identifier if known.</param>
/// <param name="CharSpacing">Additional spacing between characters in dots.</param>
/// <param name="IsBold">True when bold/emphasized mode is active.</param>
/// <param name="IsUnderline">True when underline mode is active.</param>
/// <param name="IsReverse">True when reverse (white-on-black) mode is active.</param>
public sealed record ViewTextElement(
    string Text,
    int X,
    int Y,
    int Width,
    int Height,
    string? Font,
    int CharSpacing,
    bool? IsBold,
    bool? IsUnderline,
    bool? IsReverse)
    : ViewElement
{
    /// <summary>
    /// Glyph scaling multiplier on the X axis (does not affect character spacing).
    /// </summary>
    public int CharScaleX { get; init; } = 1;

    /// <summary>
    /// Glyph scaling multiplier on the Y axis (does not affect character spacing).
    /// </summary>
    public int CharScaleY { get; init; } = 1;
}

/// <summary>
/// References media to render as an image at an absolute position in printer dots.
/// </summary>
/// <param name="Media">Descriptor for stored media bytes.</param>
/// <param name="X">Left position in dots.</param>
/// <param name="Y">Top position in dots.</param>
/// <param name="Width">Image width in dots.</param>
/// <param name="Height">Image height in dots.</param>
public sealed record ViewImageElement(
    ViewMedia Media,
    int X,
    int Y,
    int Width,
    int Height)
    : ViewElement;

/// <summary>
/// Non-visual debug element for commands, errors, and other debug information.
/// </summary>
/// <param name="DebugType">Type of debug information (e.g., command name, "error", "printerError").</param>
/// <param name="Parameters">Key/value parameters for the debug entry.</param>
public sealed record ViewDebugElement(
    string DebugType,
    IReadOnlyDictionary<string, string> Parameters)
    : ViewElement
{
    public ViewDebugElement(string debugType)
        : this(debugType, new Dictionary<string, string>())
    {
    }
}

/// <summary>
/// Descriptor for media stored in external storage for view rendering.
/// </summary>
/// <param name="ContentType">MIME type, e.g. "image/png".</param>
/// <param name="Length">Length in bytes.</param>
/// <param name="Sha256Checksum">Sha256 checksum.</param>
/// <param name="Url">Relative URL to retrieve the media bytes.</param>
public sealed record ViewMedia(
    string ContentType,
    long Length,
    string Sha256Checksum,
    string Url);
