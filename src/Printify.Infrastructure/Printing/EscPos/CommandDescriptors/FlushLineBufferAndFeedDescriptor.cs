using Printify.Infrastructure.Printing.Common;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Documents.Elements.EscPos;

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

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        return MatchResult.Matched(new PrintAndLineFeed());
    }
}
