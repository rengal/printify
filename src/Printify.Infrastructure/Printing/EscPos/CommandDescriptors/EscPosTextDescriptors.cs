using Printify.Infrastructure.Printing.Common;
using Printify.Infrastructure.Printing.EscPos.Commands;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// <summary>
/// Command: ESC ! - select print mode.
/// ASCII: ESC ! n.
/// HEX: 1B 21 n.
/// </summary>
public sealed class SetPrintModeDescriptor : ICommandDescriptor
{
    private const int FixedLength = 3;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, 0x21 };
    public int MinLength => FixedLength;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        var parameter = buffer[2];
        var fontNumber = parameter & 0x07;
        var isDoubleHeight = (parameter & 0x10) != 0;
        var isDoubleWidth = (parameter & 0x20) != 0;

        var fontElement = new EscPosSetPrintMode(fontNumber, isDoubleWidth, isDoubleHeight);
        return MatchResult.Matched(fontElement);
    }
}

/// <summary>
/// Command: ESC M n - select character font.
/// ASCII: ESC M n.
/// HEX: 1B 4D n (00=Font A, 01=Font B).
/// </summary>
public sealed class SetFontDescriptor : ICommandDescriptor
{
    private const int FixedLength = 3;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, 0x4D };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        var fontNumber = buffer[2] & 0x01; // bit0: 0=Font A, 1=Font B
        return MatchResult.Matched(new EscPosSetFont(fontNumber));
    }
}

/// <summary>
/// Command: ESC E n - enable/disable emphasized (bold) mode.
/// ASCII: ESC E n.
/// HEX: 1B 45 n (00=off, 01=on).
/// </summary>
public sealed class SetBoldModeDescriptor : ICommandDescriptor
{
    private const int FixedLength = 3;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, (byte)'E' };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        var mode = buffer[2] == 0x01;
        var element = new EscPosSetBoldMode(mode);
        return MatchResult.Matched(element);
    }
}

/// <summary>
/// Command: ESC F - cancel emphasized (bold) mode.
/// ASCII: ESC F.
/// HEX: 1B 46.
/// </summary>
public sealed class CancelBoldModeDescriptor : ICommandDescriptor
{
    private const int FixedLength = 2;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, (byte)'F' };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        return MatchResult.Matched(new EscPosCancelBoldMode());
    }
}

/// <summary>
/// Command: ESC G n - enable/disable double-strike mode.
/// ASCII: ESC G n.
/// HEX: 1B 47 n (00=off, 01=on).
/// </summary>
public sealed class SetDoubleStrikeModeDescriptor : ICommandDescriptor
{
    private const int FixedLength = 3;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, (byte)'G' };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        return MatchResult.Matched(new EscPosSetDoubleStrikeMode(buffer[2] != 0x00));
    }
}

/// <summary>
/// Command: ESC 4 - enable italic mode.
/// ASCII: ESC 4.
/// HEX: 1B 34.
/// </summary>
public sealed class EnableItalicModeDescriptor : ICommandDescriptor
{
    private const int FixedLength = 2;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, 0x34 };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        return MatchResult.Matched(new EscPosEnableItalicMode());
    }
}

/// <summary>
/// Command: ESC 5 - disable italic mode.
/// ASCII: ESC 5.
/// HEX: 1B 35.
/// </summary>
public sealed class DisableItalicModeDescriptor : ICommandDescriptor
{
    private const int FixedLength = 2;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, 0x35 };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        return MatchResult.Matched(new EscPosDisableItalicMode());
    }
}

/// <summary>
/// Command: ESC - n - enable/disable underline mode.
/// ASCII: ESC - n.
/// HEX: 1B 2D n (00=off, 01=on).
/// </summary>
public sealed class SetUnderlineModeDescriptor : ICommandDescriptor
{
    public const int FixedLength = 3;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, 0x2D };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        var enabled = buffer[2] != 0;
        var element = new EscPosSetUnderlineMode(enabled);
        return MatchResult.Matched(element);
    }
}

/// <summary>
/// Command: GS B n - enable/disable reverse (white-on-black) mode.
/// ASCII: GS B n.
/// HEX: 1D 42 n (00=off, 01=on).
/// </summary>
public sealed class SetReverseModeDescriptor : ICommandDescriptor
{
    private const int FixedLength = 3;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1D, 0x42 };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        var mode = buffer[2] == 0x01;
        var element = new EscPosSetReverseMode(mode);
        return MatchResult.Matched(element);
    }
}

/// <summary>
/// Command: GS ! n - select character size.
/// ASCII: GS ! n.
/// HEX: 1D 21 n.
/// </summary>
public sealed class SetCharacterSizeDescriptor : ICommandDescriptor
{
    private const int FixedLength = 3;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1D, 0x21 };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        var parameter = buffer[2];
        // GS ! encodes height in the low nibble and width in the high nibble, zero-based.
        var heightMultiplier = (parameter & 0x0F) + 1;
        var widthMultiplier = ((parameter >> 4) & 0x0F) + 1;
        return MatchResult.Matched(new EscPosSetCharacterSize(widthMultiplier, heightMultiplier));
    }
}

/// <summary>
/// Command: ESC SP n - set right-side character spacing.
/// ASCII: ESC SP n.
/// HEX: 1B 20 n.
/// </summary>
public sealed class SetRightCharacterSpacingDescriptor : ICommandDescriptor
{
    private const int FixedLength = 3;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, 0x20 };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        return MatchResult.Matched(new EscPosSetRightCharacterSpacing(buffer[2]));
    }
}

/// <summary>
/// Command: ESC { n - enable/disable upside-down mode.
/// ASCII: ESC { n.
/// HEX: 1B 7B n.
/// </summary>
public sealed class SetUpsideDownModeDescriptor : ICommandDescriptor
{
    private const int FixedLength = 3;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, 0x7B };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        // ESC/POS uses the least-significant bit of n for the mode.
        return MatchResult.Matched(new EscPosSetUpsideDownMode((buffer[2] & 0x01) != 0));
    }
}

/// <summary>
/// Command: HT - move to next horizontal tab stop.
/// ASCII: HT.
/// HEX: 09.
/// </summary>
public sealed class HorizontalTabDescriptor : ICommandDescriptor
{
    private const int FixedLength = 1;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x09 };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        return MatchResult.Matched(new EscPosHorizontalTab());
    }
}

/// <summary>
/// Command: ESC D n1 ... nk NUL - set horizontal tab positions.
/// ASCII: ESC D n1 ... nk NUL.
/// HEX: 1B 44 n1 ... nk 00.
/// </summary>
public sealed class SetHorizontalTabStopsDescriptor : ICommandDescriptor
{
    private static readonly byte[] PrefixBytes = [0x1B, 0x44];
    public ReadOnlyMemory<byte> Prefix => PrefixBytes;
    public int MinLength => 3;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < MinLength)
        {
            return null;
        }

        for (var i = 2; i < buffer.Length; i++)
        {
            if (buffer[i] == 0x00)
            {
                return i + 1;
            }
        }

        return null;
    }

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        var exactLength = TryGetExactLength(buffer);
        if (!exactLength.HasValue)
        {
            return MatchResult.NeedMore();
        }

        var stopCount = exactLength.Value - 3;
        var columns = new int[Math.Max(0, stopCount)];
        for (var i = 0; i < stopCount; i++)
        {
            columns[i] = buffer[i + 2];
        }

        return MatchResult.Matched(new EscPosSetHorizontalTabStops(columns));
    }
}

/// <summary>
/// Command: ESC a - select justification.
/// ASCII: ESC a n.
/// HEX: 1B 61 n (00=left, 01=center, 02=right).
/// </summary>
public sealed class SetJustificationDescriptor : ICommandDescriptor
{
    private const int FixedLength = 3;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, (byte)'a' };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        if (TryParseJustification(buffer[2], out var justification))
        {
            return MatchResult.Matched(new EscPosSetJustification(justification));
        }

        var error = new EscPosParseError("ESCPOS_PARSER_ERROR",
            $"Invalid justification value: 0x{buffer[2]:X2}. Expected 0x00 (left), 0x01 (center), or 0x02 (right)");
        return MatchResult.Matched(error);
    }

    private static bool TryParseJustification(byte value, out EscPosTextJustification result)
    {
        switch (value)
        {
            case 0x00:
                result = EscPosTextJustification.Left;
                return true;
            case 0x01:
                result = EscPosTextJustification.Center;
                return true;
            case 0x02:
                result = EscPosTextJustification.Right;
                return true;
            default:
                result = default;
                return false;
        }
    }
}

/// <summary>
/// Command: ESC 3 n - set line spacing.
/// ASCII: ESC 3 n.
/// HEX: 1B 33 0xNN.
/// </summary>
public sealed class SetLineSpacingDescriptor : ICommandDescriptor
{
    private const int FixedLength = 3;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, 0x33 };
    public int MinLength => FixedLength;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        var spacing = buffer[2];
        return MatchResult.Matched(new EscPosSetLineSpacing(spacing));
    }
}

/// <summary>
/// Command: ESC 2 - set default line spacing (approx. 30 dots).
/// ASCII: ESC 2.
/// HEX: 1B 32.
/// </summary>
public sealed class ResetLineSpacingDescriptor : ICommandDescriptor
{
    private const int FixedLength = 2;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, 0x32 };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        return MatchResult.Matched(new EscPosResetLineSpacing());
    }
}
