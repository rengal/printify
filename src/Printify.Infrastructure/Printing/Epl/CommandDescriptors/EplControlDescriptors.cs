using Printify.Infrastructure.Printing.Common;
using Printify.Infrastructure.Printing.Epl.Commands;

namespace Printify.Infrastructure.Printing.Epl.CommandDescriptors;

/// <summary>
/// Command: CR - Carriage return (no-op).
/// ASCII: CR
/// HEX: 0D
/// </summary>
public sealed class CarriageReturnDescriptor : ICommandDescriptor
{
    private const int FixedLength = 1; // Just the CR byte

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x0D }; // CR
    public int MinLength => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        const int length = 1;

        if (buffer.Length < length)
            return MatchResult.NeedMore();

        var element = new CarriageReturn();
        return EplParsingHelpers.Success(element, buffer, length);
    }
}

/// <summary>
/// Command: LF - Line feed (no-op).
/// ASCII: LF
/// HEX: 0A
/// </summary>
public sealed class LineFeedDescriptor : ICommandDescriptor
{
    private const int FixedLength = 1; // Just the LF byte

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x0A }; // LF
    public int MinLength => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        const int length = 1;

        if (buffer.Length < length)
            return MatchResult.NeedMore();

        var element = new LineFeed();
        return EplParsingHelpers.Success(element, buffer, length);
    }
}
