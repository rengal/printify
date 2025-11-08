using Printify.Domain.Documents.Elements;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: ESC a - select justification.
/// ASCII: ESC a n.
/// HEX: 1B 61 n (00=left, 01=center, 02=right).
public sealed class SetJustificationDescriptor : ICommandDescriptor
{
    private const int FixedLength = 3;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, (byte)'a' };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        var justification = ParseJustification(buffer[2]);
        if (justification is null)
            return MatchResult.NoMatch();

        return MatchResult.Matched(FixedLength, new SetJustification(justification.Value));
    }

    private TextJustification? ParseJustification(byte value)
    {
        return value switch
        {
            0x00 => TextJustification.Left,
            0x01 => TextJustification.Center,
            0x02 => TextJustification.Right,
            _ => null
        };
    }
}
