namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: GS a n - real-time printer status.
/// ASCII: GS a n.
/// HEX: 1D 61 0xNN.
public sealed class GsPrinterStatusDescriptor : ICommandDescriptor
{
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1D, 0x61 };
    public int MinLength => 3;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => buffer.Length >= MinLength ? MinLength : null;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state) => throw new NotImplementedException("GsPrinterStatusDescriptor is not wired yet.");
}
