using Printify.Domain.Documents.Elements.EscPos;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: ESC 3 n - set line spacing.
/// ASCII: ESC 3 n.
/// HEX: 1B 33 0xNN.
public sealed class SetLineSpacingDescriptor : ICommandDescriptor
{
    private const int FixedLength = 3;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, 0x33 };
    public int MinLength => FixedLength;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        var spacing = buffer[2];
        return MatchResult.Matched(new SetLineSpacing(spacing));
    }
}
