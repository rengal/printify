namespace Printify.Domain.Layout.Primitives;

/// <summary>
/// Protocol-agnostic rotation values for canvas elements.
/// Specifies rotation in 90-degree increments.
/// </summary>
public enum Rotation
{
    /// <summary>
    /// No rotation (0 degrees). Text/images render normally, left to right.
    /// </summary>
    None = 0,

    /// <summary>
    /// 90 degrees clockwise. Text/images render vertically, bottom to top.
    /// EPL: rotation=1, ESC/POS: depends on command
    /// </summary>
    Rotate90 = 90,

    /// <summary>
    /// 180 degrees. Text/images render inverted, right to left.
    /// EPL: rotation=2
    /// </summary>
    Rotate180 = 180,

    /// <summary>
    /// 270 degrees clockwise (or 90 degrees counter-clockwise). 
    /// Text/images render vertically, top to bottom.
    /// EPL: rotation=3
    /// </summary>
    Rotate270 = 270
}
