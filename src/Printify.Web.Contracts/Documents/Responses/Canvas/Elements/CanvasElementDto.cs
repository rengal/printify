using System.Text.Json.Serialization;
using Printify.Web.Contracts.Documents.Shared.Elements;

namespace Printify.Web.Contracts.Documents.Responses.Canvas.Elements;

/// <summary>
/// Base contract for canvas elements; includes debug metadata shared by all protocols.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CanvasTextElementDto), "text")]
[JsonDerivedType(typeof(CanvasImageElementDto), "image")]
[JsonDerivedType(typeof(CanvasLineElementDto), "line")]
[JsonDerivedType(typeof(CanvasBoxElementDto), "box")]
[JsonDerivedType(typeof(CanvasDebugElementDto), "debug")]
public abstract record CanvasElementDto : BaseElementDto;

/// <summary>
/// Text primitive placed at an absolute position in printer dots.
/// </summary>
public sealed record CanvasTextElementDto(
    string Text,
    int X,
    int Y,
    int Width,
    int Height,
    string? FontName,
    int CharSpacing,
    bool IsBold,
    bool IsUnderline,
    bool IsReverse,
    int CharScaleX = 1,
    int CharScaleY = 1,
    string Rotation = "none")
    : CanvasElementDto;

/// <summary>
/// Image primitive placed at an absolute position in printer dots.
/// </summary>
public sealed record CanvasImageElementDto(
    CanvasMediaDto Media,
    int X,
    int Y,
    int Width,
    int Height,
    string Rotation = "none")
    : CanvasElementDto;

/// <summary>
/// Line primitive for rendering straight lines.
/// </summary>
public sealed record CanvasLineElementDto(
    int X1,
    int Y1,
    int X2,
    int Y2,
    int Thickness)
    : CanvasElementDto;

/// <summary>
/// Box primitive for rendering rectangles.
/// </summary>
public sealed record CanvasBoxElementDto(
    int X,
    int Y,
    int Width,
    int Height,
    int Thickness)
    : CanvasElementDto;

/// <summary>
/// Non-visual debug element for commands, errors, and other diagnostics.
/// </summary>
public sealed record CanvasDebugElementDto : CanvasElementDto
{
    /// <summary>
    /// Type of debug information (e.g., command name, "error", "printerError").
    /// </summary>
    public string DebugType { get; init; }

    /// <summary>
    /// Key/value parameters for the debug entry.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string>? Parameters { get; init; }

    [JsonConstructor]
    public CanvasDebugElementDto(string debugType)
    {
        DebugType = debugType;
    }

    public CanvasDebugElementDto(string debugType, IReadOnlyDictionary<string, string>? parameters)
    {
        DebugType = debugType;
        Parameters = parameters;
    }
}

/// <summary>
/// Descriptor for media stored in external storage for canvas rendering.
/// </summary>
public sealed record CanvasMediaDto(
    string ContentType,
    int Size,
    string Url,
    string StorageKey);
