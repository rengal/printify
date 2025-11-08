namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: FS & - select Chinese (GB2312) character set.
/// ASCII: FS &.
/// HEX: 1C 26.
public sealed class SetChineseCodePageDescriptor : ICommandDescriptor
{
    private const int FixedLength = 2;
    private const string Gb2312CodePage = "936";
     
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1C, 0x26 };
    public int MinLength => FixedLength;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        return MatchResult.Matched(FixedLength, new Domain.Documents.Elements.SetCodePage(Gb2312CodePage));
    }
}
