namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: ESC ! - select font characteristics.
/// ASCII: ESC ! n.
/// HEX: 1B 21 n.
public sealed class SetFontDescriptor : ICommandDescriptor
{
    private const int FixedLength = 3;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1B, 0x21 };
    public int MinLength => FixedLength;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        var parameter = buffer[2];
        var fontNumber = parameter & 0x07;
        var isDoubleHeight = (parameter & 0x10) != 0;
        var isDoubleWidth = (parameter & 0x20) != 0;

        var fontElement = new Domain.Documents.Elements.SetFont(fontNumber, isDoubleWidth, isDoubleHeight);
        return MatchResult.Matched(FixedLength, fontElement);
    }
}
