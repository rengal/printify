using Printify.Domain.Documents.Elements;
using Printify.Domain.Documents.Elements.EscPos;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: ESC p m t1 t2 - cash drawer pulse.
/// ASCII: ESC p m t1 t2.
/// HEX: 1B 70 0xMM 0xT1 0xT2.
public sealed class PulseDescriptor : ICommandDescriptor
{
    private const int FixedLength = 5;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, (byte)'p' };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        if (buffer.Length < FixedLength)
            return MatchResult.NeedMore();

        int pin = buffer[2];
        int onTimeMs = buffer[3];
        int offTimeMs = buffer[4];

        var element = new Pulse(pin, onTimeMs, offTimeMs);
        return MatchResult.Matched(element);
    }
}
