using Printify.Domain.Documents.Elements;
using Printify.Domain.Documents.Elements.Epl;
using Printify.Infrastructure.Printing.Common;

namespace Printify.Infrastructure.Printing.Epl.CommandDescriptors;

/// <summary>
/// Command: N - Clear/acknowledge buffer.
/// ASCII: N
/// HEX: 4E
/// </summary>
public sealed class ClearBufferDescriptor : EplCommandDescriptor
{
    private const int FixedLength = 2; // 'N' + newline

    public override ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x4E }; // 'N'
    public override int MinLength => FixedLength;

    public override MatchResult TryParse(ReadOnlySpan<byte> buffer, EplParserState state)
    {
        const int length = 2;

        if (buffer.Length < length)
            return MatchResult.NeedMore();

        if (buffer[1] != (byte)'\n')
            return MatchResult.Matched(new PrinterError("N command must be followed by newline"));

        var element = new ClearBuffer();
        return EplParsingHelpers.Success(element, buffer, length);
    }
}
