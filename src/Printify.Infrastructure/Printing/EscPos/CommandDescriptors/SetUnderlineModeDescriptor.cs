using Printify.Infrastructure.Printing.Common;
using Printify.Domain.Documents.Elements.EscPos;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: ESC - n - enable/disable underline mode.
/// ASCII: ESC - n.
/// HEX: 1B 2D n (00=off, 01=on).
public sealed class SetUnderlineModeDescriptor : ICommandDescriptor
{
    public const int FixedLength = 3;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, 0x2D };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        var enabled = buffer[2] != 0;
        var element = new SetUnderlineMode(enabled);
        return MatchResult.Matched(element);
    }
}
