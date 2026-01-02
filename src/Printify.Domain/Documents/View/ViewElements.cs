using System.Collections.Generic;

namespace Printify.Domain.Documents.View;

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
    : ViewElement;

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
/// Non-visual element that only mutates state.
/// </summary>
/// <param name="StateName">Name of the state change.</param>
/// <param name="Parameters">Key/value parameters for the state change.</param>
public sealed record ViewStateElement(
    string StateName,
    IReadOnlyDictionary<string, string> Parameters)
    : ViewElement;

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
