using System.Text.Json.Serialization;
using Printify.Web.Contracts.Documents.Shared.Elements;

namespace Printify.Web.Contracts.Documents.Responses.View.Elements;

/// <summary>
/// Canonical font identifiers for view-oriented rendering.
/// </summary>
public static class ViewFontNames
{
    public const string EscPosA = "ESCPOS_A";
    public const string EscPosB = "ESCPOS_B";
}

/// <summary>
/// Base contract for view-oriented elements; includes debug metadata shared by all protocols.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ViewTextElementDto), "text")]
[JsonDerivedType(typeof(ViewImageElementDto), "image")]
[JsonDerivedType(typeof(ViewStateElementDto), "none")]
public abstract record ViewElementDto : BaseElementDto
{
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
public sealed record ViewTextElementDto(
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
    : ViewElementDto;

/// <summary>
/// References media to render as an image at an absolute position in printer dots.
/// </summary>
/// <param name="Media">Descriptor for stored media bytes.</param>
/// <param name="X">Left position in dots.</param>
/// <param name="Y">Top position in dots.</param>
/// <param name="Width">Image width in dots.</param>
/// <param name="Height">Image height in dots.</param>
public sealed record ViewImageElementDto(
    ViewMediaDto Media,
    int X,
    int Y,
    int Width,
    int Height)
    : ViewElementDto;

/// <summary>
/// Non-visual element that only mutates state; emitted as "type":"none".
/// </summary>
/// <param name="StateName">Name of the state change.</param>
/// <param name="Parameters">Key/value parameters for the state change.</param>
public sealed record ViewStateElementDto(
    string StateName,
    IReadOnlyDictionary<string, string> Parameters)
    : ViewElementDto;

/// <summary>
/// Descriptor for media stored in external storage for view rendering.
/// </summary>
/// <param name="ContentType">MIME type, e.g. "image/png".</param>
/// <param name="Length">Length in bytes.</param>
/// <param name="Sha256">Sha256 checksum.</param>
/// <param name="Url">Relative URL to retrieve the media bytes.</param>
public sealed record ViewMediaDto(
    string ContentType,
    long Length,
    string Sha256,
    string Url);
