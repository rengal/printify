using Printify.Infrastructure.Printing.Common;
using Printify.Infrastructure.Printing.EscPos.Commands;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// <summary>
/// Command: Line Feed - print buffer and feed one line.
/// ASCII: LF.
/// HEX: 0A.
/// </summary>
public sealed class FlushLineBufferAndFeedDescriptor : ICommandDescriptor
{
    private readonly int fixedLength = 1;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x0A };
    public int MinLength => fixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => fixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        return MatchResult.Matched(new EscPosPrintAndLineFeed());
    }
}

/// <summary>
/// Command: Carriage Return (CR) - legacy compatibility command, ignored by printer.
/// ASCII: CR.
/// HEX: 0D.
/// </summary>
public sealed class LegacyCarriageReturnDescriptor : ICommandDescriptor
{
    private readonly int fixedLength = 1;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x0D };
    public int MinLength => fixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => fixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        // Compatibility workaround: emit a non-visual element so debug traces include CR.
        return MatchResult.Matched(new EscPosLegacyCarriageReturn());
    }
}

/// <summary>
/// Command: GS V m [n] - paper cut with mode.
/// ASCII: GS V m [n].
/// HEX: 1D 56 0xMM [0xNN].
/// </summary>
public sealed class PageCutDescriptor : ICommandDescriptor
{
    private const int MinimumLength = 3;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1D, 0x56 };

    public int MinLength => 3;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < MinimumLength)
        {
            return null;
        }

        var m = buffer[2];
        return GetCommandLength(m);
    }

    private static int? GetCommandLength(byte mode)
    {
        return mode switch
        {
            // Function A: Cuts the paper (no feed parameter)
            0 or 48 => 3,

            // Function B: Feeds paper and cuts (has feed parameter n)
            65 or 66 => 4,

            // Function C: Sets cutting position (has feed parameter n)
            97 or 98 => 4,

            // Function D: Feeds, cuts, and feeds to print start (has feed parameter n)
            103 or 104 => 4,

            // Unknown/unsupported mode
            _ => null
        };
    }

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < MinimumLength)
        {
            return MatchResult.NeedMore();
        }

        // Mode byte (m) is at buffer[2]
        var modeValue = buffer[2];

        // Determine bytes to consume and cut mode
        var bytesToConsume = GetCommandLength(modeValue) ?? 3;

        // Extract feed parameter if present (4-byte commands have feed parameter at buffer[3])
        int? feedMotionUnits = bytesToConsume == 4 && buffer.Length >= 4 ? buffer[3] : null;

        // Map ESC/POS mode byte to PagecutMode enum
        var cutMode = modeValue switch
        {
            0 or 48 or 65 or 97 or 103 => EscPosPagecutMode.Full,
            1 or 49 or 66 or 98 or 104 => EscPosPagecutMode.Partial,
            _ => EscPosPagecutMode.Full // Default to full cut for unknown modes
        };

        // Create CutPaper element with the determined mode and feed parameter
        var element = new EscPosCutPaper(cutMode, feedMotionUnits);

        return MatchResult.Matched(element);
    }
}

/// <summary>
/// Command: Partial cut (one point left uncut)
/// ASCII: ESC i
/// HEX: 1B 69
/// </summary>
public sealed class PartialCutOnePointDescriptor : ICommandDescriptor
{
    private readonly int fixedLength = 2;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, (byte)'i' };

    public int MinLength => fixedLength;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => fixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        var element = new EscPosCutPaper(EscPosPagecutMode.PartialOnePoint);
        return MatchResult.Matched(element);
    }
}

/// <summary>
/// Command: Partial cut (three points left uncut)
/// ASCII: ESC m
/// HEX: 1B 6D
/// </summary>
public sealed class PartialCutThreePointDescriptor : ICommandDescriptor
{
    private readonly int fixedLength = 2;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, (byte)'m' };

    public int MinLength => fixedLength;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => fixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        var element = new EscPosCutPaper(EscPosPagecutMode.PartialThreePoint);
        return MatchResult.Matched(element);
    }
}
