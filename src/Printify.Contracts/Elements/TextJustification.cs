namespace Printify.Contracts.Elements;

/// <summary>
/// Supported text alignment values for ESC a (justification) commands.
/// </summary>
public enum TextJustification
{
    /// <summary>
    /// Align text to the left margin.
    /// </summary>
    Left = 0,

    /// <summary>
    /// Center text relative to the printable width.
    /// </summary>
    Center = 1,

    /// <summary>
    /// Align text to the right margin.
    /// </summary>
    Right = 2
}
