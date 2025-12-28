using Printify.Domain.Documents.Elements;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: DLE EOT n - real-time printer status.
/// ASCII: DLE EOT n.
/// HEX: 10 04 n.
public sealed class GetPrinterStatusDescriptor: ICommandDescriptor
{
    private const int FixedLength = 3;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x10, 0x04 };
    public int MinLength => 3;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer)
    {
        byte statusByte = buffer[2];

        // For status bytes 0x01-0x04, length is 3
        if (statusByte is >= 0x01 and <= 0x04)
            return 3;

        // For status bytes 0x07, 0x08, 0x12, need 4 bytes
        if (statusByte is 0x07 or 0x08 or 0x12)
            return buffer.Length >= 4 ? 4 : null;

        // Unknown status byte, cannot determine length
        return null;
    }

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        byte statusByte = buffer[2];

        if (statusByte is >= 0x01 and <= 0x04)
        {
            var element = new GetPrinterStatus(statusByte);
            return MatchResult.Matched(element);
        }

        if (statusByte is 0x07 or 0x08 or 0x12)
        {
            var additionalStatusByte = buffer[3];
            var element = new GetPrinterStatus(statusByte, additionalStatusByte);
            return MatchResult.Matched(element);
        }

        return MatchResult.Error(MatchKind.ErrorInvalid);
    }
}

