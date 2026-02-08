using Printify.Infrastructure.Printing.Common;
using Printify.Infrastructure.Printing.Epl.Commands;

namespace Printify.Infrastructure.Printing.Epl.CommandDescriptors;

/// <summary>
/// Command: N - Clear/acknowledge buffer.
/// ASCII: N
/// HEX: 4E
/// </summary>
public sealed class ClearBufferDescriptor : ICommandDescriptor
{
    private const int FixedLength = 2; // 'N' + newline

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x4E }; // 'N'
    public int MinLength => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        const int length = 2;

        if (buffer.Length < length)
            return MatchResult.NeedMore();

        var terminatorByte = buffer[1];
        if (terminatorByte != 0x0A && terminatorByte != 0x0D) // LF or CR
            return MatchResult.Matched(new EplParseError("EPL_PARSER_ERROR", "N command must be followed by CR or LF"));

        var element = new EplClearBuffer();
        return EplParsingHelpers.Success(element, buffer, length);
    }
}
