using Printify.Domain.Documents.Elements;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// <summary>
/// Command: Partial cut (three points left uncut)
/// ASCII: ESC m
/// HEX: 1B 6D
/// </summary>
public sealed class PartialCutThreePointDescriptor : ICommandDescriptor
{
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, (byte)'m' };

    public int MinLength => 2;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer)
    {
        return MinLength;
    }

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        var prefix = Prefix.Span;
        for (var i = 0; i < prefix.Length; i++)
        {
            if (buffer[i] != prefix[i])
            {
                return MatchResult.NoMatch();
            }
        }

        state.FlushText(allowEmpty: false);
        var element = state.CreateElement(sequence => new Pagecut(sequence));
        return MatchResult.Matched(prefix.Length, element);
    }
}
