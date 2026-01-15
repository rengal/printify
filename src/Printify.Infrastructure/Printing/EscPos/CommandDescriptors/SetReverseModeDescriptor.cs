using Printify.Infrastructure.Printing.Common;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Documents.Elements.EscPos;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: GS B n - enable/disable reverse (white-on-black) mode.
/// ASCII: GS B n.
/// HEX: 1D 42 n (00=off, 01=on).
public sealed class SetReverseModeDescriptor : ICommandDescriptor
{
    private const int FixedLength = 3;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1D, 0x42 };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        var mode = buffer[2] == 0x01;
        var element = new SetReverseMode(mode);
        return MatchResult.Matched(element);
    }
}
