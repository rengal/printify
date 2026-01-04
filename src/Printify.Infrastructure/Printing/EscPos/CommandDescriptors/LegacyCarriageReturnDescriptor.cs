using Printify.Domain.Documents.Elements;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: Carriage Return (CR) - legacy compatibility command, ignored by printer.
/// ASCII: CR.
/// HEX: 0D.
public sealed class LegacyCarriageReturnDescriptor : ICommandDescriptor
{
    private readonly int fixedLength = 1;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x0D };
    public int MinLength => fixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => fixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        // Compatibility workaround: emit a non-visual element so debug traces include CR.
        return MatchResult.Matched(new LegacyCarriageReturn());
    }
}
