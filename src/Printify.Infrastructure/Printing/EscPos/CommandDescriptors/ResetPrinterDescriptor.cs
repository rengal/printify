namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: ESC @ - reset printer.
/// ASCII: ESC @.
/// HEX: 1B 40.
public sealed class ResetPrinterDescriptor : ICommandDescriptor
{
    private const int FixedLength = 2;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, 0x40 };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        return MatchResult.Matched(new Domain.Documents.Elements.ResetPrinter());
    }
}
