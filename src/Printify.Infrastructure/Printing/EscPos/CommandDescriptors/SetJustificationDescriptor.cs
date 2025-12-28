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
        return TryParseJustification(buffer[2], out var justification)
            ? MatchResult.Matched(new SetJustification(justification))
            : MatchResult.Error(MatchKind.ErrorInvalid);
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
