namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: Line Feed - output current line and start a new one.
/// ASCII: LF.
/// HEX: 0A.
public sealed class LineFeedDescriptor : ICommandDescriptor
{
    private readonly int fixedLength = 1;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x0A };
    public int MinLength => fixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => fixedLength;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state) => MatchResult.Matched(fixedLength, null);
}
