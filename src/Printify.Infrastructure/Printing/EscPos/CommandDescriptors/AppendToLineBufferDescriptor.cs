namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

using EscPos;

public sealed class AppendToLineBufferDescriptor : ICommandDescriptor
{
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { };
    public int MinLength => 1;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => null;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        // Count consecutive bytes until we hit a control character
        var length = 0;
        var endsWithTerminator = false;

        foreach (var value in buffer)
        {
            // Check if byte is a control character
            if (EscPosControlCharacters.TextTerminators.Contains(value))
            {
                endsWithTerminator = true;
                break;
            }

            length++;
        }

        if (length == 0)
            return MatchResult.NoMatch();

        // Extract the text bytes and convert to string
        var textBytes = buffer.Slice(0, length);
        var text = state.Encoding.GetString(textBytes);
        var element = new Domain.Documents.Elements.AppendToLineBuffer(text);

        // If we hit a terminator, this line is complete (Matched)
        // If we reached the end of buffer without a terminator, more text may follow (MatchedPending)
        return endsWithTerminator
            ? MatchResult.Matched(length, element)
            : MatchResult.MatchedPending(length, element);
    }
}
