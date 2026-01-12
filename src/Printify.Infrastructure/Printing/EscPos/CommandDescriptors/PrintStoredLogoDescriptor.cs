using Printify.Infrastructure.Printing.Common;
using Printify.Domain.Documents.Elements.EscPos;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: FS p m n - print stored logo by identifier.
/// ASCII: FS p m n.
/// HEX: 1C 70 m n.
public sealed class PrintStoredLogoDescriptor : ICommandDescriptor
{
    private const int FixedLength = 4;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1C, (byte)'p' };
    public int MinLength => FixedLength;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        // logoId is the fourth byte (index 3)
        var logoId = buffer[3];
        var element = new StoredLogo(logoId);
        return MatchResult.Matched(element);
    }
}
