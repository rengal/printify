using Printify.Domain.Documents.Elements;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// <summary>
/// Command: Partial cut (one point left uncut)
/// ASCII: ESC i
/// HEX: 1B 69
/// </summary>
public sealed class PartialCutOnePointDescriptor : ICommandDescriptor
{
    private readonly int fixedLength = 2;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, (byte)'i' };

    public int MinLength => fixedLength;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => fixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        var element = new Pagecut();
        return MatchResult.Matched(fixedLength, element);
    }
}
