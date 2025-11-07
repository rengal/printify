namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: GS V m [n] - paper cut with mode.
/// ASCII: GS V m [n].
/// HEX: 1D 56 0xMM [0xNN].
public sealed class GsCutDescriptor : ICommandDescriptor
{
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1D, 0x56 };
    public int MinLength => 3;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => null;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state) => throw new NotImplementedException("GsCutDescriptor is not wired yet.");
}
