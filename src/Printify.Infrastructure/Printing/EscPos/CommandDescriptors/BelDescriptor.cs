namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: BEL - buzzer/beeper.
/// ASCII: BEL.
/// HEX: 07.
public sealed class BelDescriptor : ICommandDescriptor
{
    private const int fixedLength = 1;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x07 };
    public int MinLength => fixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => fixedLength;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state) => MatchResult.Matched(fixedLength, new Domain.Documents.Elements.Bell());
}
