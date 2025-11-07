namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: ESC p m t1 t2 - cash drawer pulse.
/// ASCII: ESC p m t1 t2.
/// HEX: 1B 70 0xMM 0xT1 0xT2.
public sealed class EscPulseDescriptor : ICommandDescriptor
{
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, (byte)'p' };
    public int MinLength => 5;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => buffer.Length >= MinLength ? MinLength : null;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state) => throw new NotImplementedException("EscPulseDescriptor is not wired yet.");
}
