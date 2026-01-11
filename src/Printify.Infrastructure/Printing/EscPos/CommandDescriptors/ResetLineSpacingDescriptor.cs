using Printify.Domain.Documents.Elements.EscPos;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: ESC 2 - set default line spacing (approx. 30 dots).
/// ASCII: ESC 2.
/// HEX: 1B 32.
public sealed class ResetLineSpacingDescriptor : ICommandDescriptor
{
    private const int FixedLength = 2;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, 0x32 };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        return MatchResult.Matched(new ResetLineSpacing());
    }
}
