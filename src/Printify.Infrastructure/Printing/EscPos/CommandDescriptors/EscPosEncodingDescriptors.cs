using Printify.Infrastructure.Printing.Common;
using Printify.Infrastructure.Printing.EscPos.Commands;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// <summary>
/// Command: ESC t - select character code table.
/// ASCII: ESC t n.
/// HEX: 1B 74 n.
/// </summary>
public sealed class SetCodePageDescriptor : ICommandDescriptor
{
    private const int FixedLength = 3;
    private static readonly IReadOnlyDictionary<byte, string> EscCodePageMap = new Dictionary<byte, string>
    {
        [0x00] = "437",
        [0x20] = "720",
        [0x0E] = "737",
        [0x21] = "775",
        [0x02] = "850",
        [0x12] = "852",
        [0x22] = "855",
        [0x0D] = "857",
        [0x13] = "858",
        [0x03] = "860",
        [0x23] = "861",
        [0x24] = "862",
        [0x04] = "863",
        [0x25] = "864",
        [0x05] = "865",
        [0x11] = "866",
        [0x26] = "869",
        [0x29] = "1098",
        [0x2A] = "1118",
        [0x2B] = "1119",
        [0x2C] = "1125",
        [0x2D] = "1250",
        [0x2E] = "1251",
        [0x10] = "1252",
        [0x2F] = "1253",
        [0x30] = "1254",
        [0x31] = "1255",
        [0x32] = "1256",
        [0x33] = "1257",
        [0x34] = "1258"
    };

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, (byte)'t' };
    public int MinLength => FixedLength;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        var codePageId = buffer[2];
        if (EscCodePageMap.TryGetValue(codePageId, out var codePage))
        {
            return MatchResult.Matched(new EscPosSetCodePage(codePage));
        }

        var error = new EscPosParseError("ESCPOS_PARSE_ERROR", $"Unrecognized code page ID: 0x{codePageId:X2}");
        return MatchResult.Matched(error);
    }
}

/// <summary>
/// Command: FS & - select Chinese (GB2312) character set.
/// ASCII: FS &.
/// HEX: 1C 26.
/// </summary>
public sealed class SetChineseCodePageDescriptor : ICommandDescriptor
{
    private const int FixedLength = 2;
    private const string Gb2312CodePage = "936";

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1C, 0x26 };
    public int MinLength => FixedLength;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        return MatchResult.Matched(new EscPosSetCodePage(Gb2312CodePage));
    }
}
