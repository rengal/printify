using Printify.Domain.Documents.Elements;
using Printify.Domain.Documents.Elements.Epl;
using Printify.Infrastructure.Printing.Common;

namespace Printify.Infrastructure.Printing.Epl.CommandDescriptors;

/// Command: N - Clear/acknowledge buffer.
/// ASCII: N
/// HEX: 4E
public sealed class EplNNAcknowledgeDescriptor : ICommandDescriptor<EplParserState>
{
    private const int FixedLength = 2; // 'N' + newline

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x4E }; // 'N'
    public int MinLength => FixedLength;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 2)
            return null;
        // N is always followed by newline
        return buffer[1] == (byte)'\n' ? 2 : null;
    }

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, EplParserState state)
    {
        const int length = 2;

        if (buffer.Length < length)
            return MatchResult.NeedMore();

        if (buffer[1] != (byte)'\n')
            return MatchResult.Matched(new PrinterError("N command must be followed by newline"));

        var element = new ClearBuffer()
        {
            CommandRaw = Convert.ToHexString(buffer[..length]),
            LengthInBytes = length
        };
        return MatchResult.Matched(element);
    }
}
