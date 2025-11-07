namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: FS & - select Chinese (GB2312) character set.
/// ASCII: FS &.
/// HEX: 1C 26.
public sealed class FsSelectChineseCharsetDescriptor : ICommandDescriptor
{
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1C, 0x26 };
    public int MinLength => 2;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => MinLength;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state) => throw new NotImplementedException("FsSelectChineseCharsetDescriptor is not wired yet.");
}
