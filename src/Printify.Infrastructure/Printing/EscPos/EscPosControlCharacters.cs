namespace Printify.Infrastructure.Printing.EscPos;

/// <summary>
/// Defines well-known ESC/POS control characters and shared control sets.
/// </summary>
public static class EscPosControlCharacters
{
    public const byte Bell = 0x07;
    public const byte HorizontalTab = 0x09;
    public const byte LineFeed = 0x0A;
    public const byte CarriageReturn = 0x0D;
    public const byte DataLinkEscape = 0x10;
    public const byte Escape = 0x1B;
    public const byte GroupSeparator = 0x1D;
    public const byte FileSeparator = 0x1C;

    private static readonly byte[] textTerminatorBytes =
    [
        Bell,
        HorizontalTab,
        LineFeed,
        CarriageReturn,
        DataLinkEscape,
        Escape,
        GroupSeparator,
        FileSeparator
    ];

    /// <summary>
    /// Returns the control characters that terminate a text run.
    /// </summary>
    public static ReadOnlySpan<byte> TextTerminators => textTerminatorBytes;
}
