using Printify.Domain.Documents.Elements;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: Line Feed - print buffer and feed one line.
/// ASCII: LF.
/// HEX: 0A.
public sealed class FlushLineBufferAndFeedDescriptor : ICommandDescriptor
{
    private readonly int fixedLength = 1;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x0A };
    public int MinLength => fixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => fixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        return MatchResult.Matched(new FlushLineBufferAndFeed());
    }
}
