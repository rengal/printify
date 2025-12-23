using System;
using Printify.Domain.Documents.Elements;
using Printify.Infrastructure.Printing.EscPos;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

public sealed class ErrorDescriptor : ICommandDescriptor
{
    private readonly IReadOnlySet<byte> commandPrefixBytes;

    public ErrorDescriptor(IReadOnlySet<byte> commandPrefixBytes)
    {
        ArgumentNullException.ThrowIfNull(commandPrefixBytes);
        this.commandPrefixBytes = commandPrefixBytes;
    }

    public ReadOnlyMemory<byte> Prefix { get; } = ReadOnlyMemory<byte>.Empty;
    public int MinLength => 1;

    public bool PrefixAcceptsNext(byte value)
    {
        return !EscPosTextByteRules.IsTextByte(value) && !commandPrefixBytes.Contains(value);
    }

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer)
    {
        return buffer.Length > 0 ? buffer.Length : null;
    }

    public MatchResult TryParse2(ReadOnlySpan<byte> buffer, ParserState state)
    {
        if (buffer.IsEmpty)
        {
            return MatchResult.NoMatch();
        }

        var element = new PrinterError($"Unrecognized {buffer.Length} bytes");
        return MatchResult.MatchedPending(buffer.Length, element);
    }

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        // Count consecutive bytes until we hit a non-text byte.
        var length = 0;
        var endsWithTerminator = false;

        foreach (var value in buffer)
        {
            if (!PrefixAcceptsNext(value))
            {
                endsWithTerminator = true;
                break;
            }

            length++;
        }

        if (length == 0)
            return MatchResult.NoMatch();

        var element = new PrinterError($"Unrecognized {buffer.Length} bytes");

        // If we hit a terminator, this line is complete (Matched)
        // If we reached the end of buffer without a terminator, more text may follow (MatchedPending)
        return endsWithTerminator
            ? MatchResult.Matched(length, element)
            : MatchResult.MatchedPending(length, element);
    }
}
