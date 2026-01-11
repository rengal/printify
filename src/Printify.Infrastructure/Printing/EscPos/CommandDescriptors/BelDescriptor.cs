using Printify.Domain.Documents.Elements.EscPos;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: BEL - buzzer/beeper.
/// ASCII: BEL.
/// HEX: 07.
public sealed class BelDescriptor : ICommandDescriptor
{
    private const int FixedLength = 1;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x07 };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state) => MatchResult.Matched(new Bell());
}
