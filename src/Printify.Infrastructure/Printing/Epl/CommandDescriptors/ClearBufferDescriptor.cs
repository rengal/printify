using Printify.Domain.Printing;
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

        if (buffer[1] != (byte)'\n')
            return MatchResult.Matched(new PrinterError("N command must be followed by newline"));

        var element = new ClearBuffer();
        return EplParsingHelpers.Success(element, buffer, length);
    }
}
