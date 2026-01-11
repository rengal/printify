using Printify.Domain.Documents.Elements;
using Printify.Domain.Documents.Elements.EscPos;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: ESC E n - enable/disable emphasized (bold) mode.
/// ASCII: ESC E n.
/// HEX: 1B 45 n (00=off, 01=on).
public sealed class SetBoldModeDescriptor : ICommandDescriptor
{
    private const int FixedLength = 3;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, (byte)'E' };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        var mode = buffer[2] == 0x01;
        var element = new SetBoldMode(mode);
        return MatchResult.Matched(element);
    }
}
