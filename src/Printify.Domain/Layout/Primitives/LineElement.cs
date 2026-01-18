namespace Printify.Domain.Layout.Primitives;

/// <summary>
/// Line primitive (horizontal, vertical, diagonal).
/// </summary>
public sealed record LineElement(
    int X1,
    int Y1,
    int X2,
    int Y2,
    int Thickness) : BaseElement;
