namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: ESC 2 - set default line spacing (approx. 30 dots).
/// ASCII: ESC 2.
/// HEX: 1B 32.
public sealed class EscResetLineSpacingDescriptor : ICommandDescriptor
{
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, 0x32 };
    public int MinLength => 2;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => MinLength;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state) => throw new NotImplementedException("EscResetLineSpacingDescriptor is not wired yet.");
}
