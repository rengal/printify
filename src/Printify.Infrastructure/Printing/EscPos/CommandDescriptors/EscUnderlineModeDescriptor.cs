namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: ESC - n - enable/disable underline mode.
/// ASCII: ESC - n.
/// HEX: 1B 2D 0xNN (00=off, 01=on).
public sealed class EscUnderlineModeDescriptor : ICommandDescriptor
{
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, 0x2D };
    public int MinLength => 3;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => buffer.Length >= MinLength ? MinLength : null;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state) => throw new NotImplementedException("EscUnderlineModeDescriptor is not wired yet.");
}
