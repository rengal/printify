namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: ESC 3 n - set line spacing.
/// ASCII: ESC 3 n.
/// HEX: 1B 33 0xNN.
public sealed class EscSetLineSpacingDescriptor : ICommandDescriptor
{
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, 0x33 };
    public int MinLength => 3;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => buffer.Length >= MinLength ? MinLength : null;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state) => throw new NotImplementedException("EscSetLineSpacingDescriptor is not wired yet.");
}
