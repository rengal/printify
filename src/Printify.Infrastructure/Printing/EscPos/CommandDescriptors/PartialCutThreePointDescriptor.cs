using Printify.Domain.Documents.Elements;
using Printify.Domain.Documents.Elements.EscPos;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// <summary>
/// Command: Partial cut (three points left uncut)
/// ASCII: ESC m
/// HEX: 1B 6D
/// </summary>
public sealed class PartialCutThreePointDescriptor : ICommandDescriptor
{
    private readonly int fixedLength = 2;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, (byte)'m' };

    public int MinLength => fixedLength;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => fixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        var element = new CutPaper(PagecutMode.PartialThreePoint);
        return MatchResult.Matched(element);
    }
}
