namespace Printify.Domain.Layout.Primitives;

/// <summary>
/// Text primitive with position, font, and styling.
/// Supports features from ESC/POS, EPL, ZPL, TSPL: rotation, scaling, bold, underline, reverse.
/// Decoupled from protocol commands - represents final rendered text, not the command that produced it.
/// </summary>
public sealed record TextElement(
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
    int CharScaleX,
    int CharScaleY,
    Rotation Rotation) : BaseElement;
