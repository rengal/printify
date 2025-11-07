namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: ESC @ - reset printer.
/// ASCII: ESC @.
/// HEX: 1B 40.
public sealed class EscResetPrinterDescriptor : ICommandDescriptor
{
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, 0x40 };
    public int MinLength => 2;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => MinLength;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state) => throw new NotImplementedException("EscResetPrinterDescriptor is not wired yet.");
}
