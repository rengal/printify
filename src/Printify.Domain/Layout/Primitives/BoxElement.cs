namespace Printify.Domain.Layout.Primitives;

/// <summary>
/// Box/rectangle primitive (outline or filled).
/// </summary>
public sealed record BoxElement(
    int X,
    int Y,
    int Width,
    int Height,
    int Thickness,
    bool IsFilled) : BaseElement;
