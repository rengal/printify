namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: ESC a n - select justification.
/// ASCII: ESC a n.
/// HEX: 1B 61 0xNN (00=left, 01=center, 02=right).
public sealed class EscJustificationDescriptor : ICommandDescriptor
{
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, (byte)'a' };
    public int MinLength => 3;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => buffer.Length >= MinLength ? MinLength : null;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state) => throw new NotImplementedException("EscJustificationDescriptor is not wired yet.");
}
