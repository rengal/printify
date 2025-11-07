namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: ESC ! n - select font characteristics.
/// ASCII: ESC ! n.
/// HEX: 1B 21 0xNN.
public sealed class EscSelectFontDescriptor : ICommandDescriptor
{
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, 0x21 };
    public int MinLength => 3;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => buffer.Length >= MinLength ? MinLength : null;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state) => throw new NotImplementedException("EscSelectFontDescriptor is not wired yet.");
}
