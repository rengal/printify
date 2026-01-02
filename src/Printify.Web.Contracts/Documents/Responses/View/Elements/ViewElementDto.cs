using System.Collections.Generic;
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
[JsonDerivedType(typeof(ViewStateElementDto), "debug")]
public abstract record ViewElementDto : BaseElementDto
{
    /// <summary>
    /// Explicit stacking order within the page; higher values render on top.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
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
    : ViewElementDto
{
    /// <summary>
    /// Glyph scaling multiplier on the X axis (does not affect character spacing).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CharScaleX { get; init; }

    /// <summary>
    /// Glyph scaling multiplier on the Y axis (does not affect character spacing).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CharScaleY { get; init; }
}

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
/// Non-visual element that only mutates state; emitted as "type":"debug".
/// </summary>
public sealed record ViewStateElementDto : ViewElementDto
{
    /// <summary>
    /// Name of the state change.
    /// </summary>
    public string StateName { get; init; }

    /// <summary>
    /// Key/value parameters for the state change.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyDictionary<string, string>? Parameters { get; init; }

    [JsonConstructor]
    public ViewStateElementDto(string stateName)
    {
        StateName = stateName;
    }

    public ViewStateElementDto(string stateName, IReadOnlyDictionary<string, string> parameters)
    {
        StateName = stateName;
        Parameters = parameters;
    }
}

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
