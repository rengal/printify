using Printify.Domain.Printing;
using Printify.Infrastructure.Printing.Common;
using Printify.Infrastructure.Printing.EscPos.Commands;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// <summary>
/// Command: ESC ! - select font characteristics.
/// ASCII: ESC ! n.
/// HEX: 1B 21 n.
/// </summary>
public sealed class SetFontDescriptor : ICommandDescriptor
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

        var fontElement = new SelectFont(fontNumber, isDoubleWidth, isDoubleHeight);
        return MatchResult.Matched(fontElement);
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
        var element = new SetBoldMode(mode);
        return MatchResult.Matched(element);
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
        var element = new SetUnderlineMode(enabled);
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
        var element = new SetReverseMode(mode);
        return MatchResult.Matched(element);
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
            return MatchResult.Matched(new SetJustification(justification));
        }

        var error = new PrinterError($"Invalid justification value: 0x{buffer[2]:X2}. Expected 0x00 (left), 0x01 (center), or 0x02 (right)");
        return MatchResult.Matched(error);
    }

    private static bool TryParseJustification(byte value, out TextJustification result)
    {
        switch (value)
        {
            case 0x00:
                result = TextJustification.Left;
                return true;
            case 0x01:
                result = TextJustification.Center;
                return true;
            case 0x02:
                result = TextJustification.Right;
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
        return MatchResult.Matched(new SetLineSpacing(spacing));
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
        return MatchResult.Matched(new ResetLineSpacing());
    }
}
