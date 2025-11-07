namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: ESC t n - select character code table.
/// ASCII: ESC t n.
/// HEX: 1B 74 0xNN.
public sealed class EscSelectCodePageDescriptor : ICommandDescriptor
{
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, (byte)'t' };
    public int MinLength => 3;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => buffer.Length >= MinLength ? MinLength : null;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state) => throw new NotImplementedException("EscSelectCodePageDescriptor is not wired yet.");
}
