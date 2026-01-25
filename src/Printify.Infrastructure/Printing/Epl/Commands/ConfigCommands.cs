namespace Printify.Infrastructure.Printing.Epl.Commands;

/// <summary>
/// Print direction for EPL printers.
/// </summary>
public enum PrintDirection
{
    /// <summary>Top to bottom (normal printing direction).</summary>
    TopToBottom,
    /// <summary>Bottom to top (rotated 180°).</summary>
    BottomToTop
}

/// <summary>
/// Clear buffer (acknowledge/clear image buffer).
/// Command: N
/// </summary>
public sealed record ClearBuffer : EplCommand;

/// <summary>
/// Set label width in dots.
/// Command: q width
/// </summary>
/// <param name="Width">Label width in dots.</param>
public sealed record SetLabelWidth(int Width) : EplCommand;

/// <summary>
/// Set label height in dots.
/// Command: Q height, [ignored]
/// </summary>
/// <param name="Height">Label height in dots.</param>
/// <param name="SecondParameter">Second parameter (typically 26, often ignored).</param>
public sealed record SetLabelHeight(int Height, int SecondParameter = 0) : EplCommand;

/// <summary>
/// Set print speed.
/// Command: R speed (where speed is typically 2-5)
/// </summary>
/// <param name="Speed">Print speed (inches per second).</param>
public sealed record SetPrintSpeed(int Speed) : EplCommand;

/// <summary>
/// Set print darkness.
/// Command: S darkness (typically 0-15)
/// </summary>
/// <param name="Darkness">Print darkness value.</param>
public sealed record SetPrintDarkness(int Darkness) : EplCommand;

/// <summary>
/// Set print direction.
/// Command: Z T (top to bottom) or Z B (bottom to top)
/// </summary>
/// <param name="Direction">Direction: TopToBottom or BottomToTop.</param>
public sealed record SetPrintDirection(PrintDirection Direction) : EplCommand;

/// <summary>
/// Set international character set/codepage.
/// Command: I code
/// </summary>
/// <param name="Code">Character set/codepage number (e.g., 8 for DOS 866 Cyrillic).</param>
public sealed record SetInternationalCharacter(int Code) : EplCommand;

/// <summary>
/// Set code page (alternative to I command).
/// Command: i code, scaling
/// </summary>
/// <param name="Code">Codepage number (e.g., 38 for DOS 866).</param>
/// <param name="Scaling">Character scaling (0-9).</param>
public sealed record SetCodePage(int Code, int Scaling = 0) : EplCommand;

/// <summary>
/// Carriage return (no-op command for debug/logging).
/// Command: CR (0x0D)
/// </summary>
public sealed record CarriageReturn : EplCommand;

/// <summary>
/// Line feed (no-op command for debug/logging).
/// Command: LF (0x0A)
/// </summary>
public sealed record LineFeed : EplCommand;
