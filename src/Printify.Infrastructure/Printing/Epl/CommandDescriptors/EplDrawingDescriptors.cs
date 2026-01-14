using Printify.Domain.Documents.Elements;
using Printify.Domain.Documents.Elements.Epl;
using Printify.Infrastructure.Printing.Common;

namespace Printify.Infrastructure.Printing.Epl.CommandDescriptors;

/// <summary>
/// Command: A x, y, rotation, font, h-mul, v-mul, reverse, "text" - Scalable/rotatable text.
/// ASCII: A {x},{y},{rotation},{font},{h},{v},{reverse},"{text}"
/// HEX: 41 {x},{y},{rotation},{font},{h},{v},{reverse},{text}
/// </summary>
public sealed class ScalableTextDescriptor : EplCommandDescriptor
{
    private const int MinLen = 10; // 'A' + minimum params

    public override ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x41 }; // 'A'
    public override int MinLength => MinLen;

    public override MatchResult TryParse(ReadOnlySpan<byte> buffer, EplParserState state)
    {
        if (!EplParsingHelpers.TryFindNewlineFromEnd(buffer, out var newline))
            return MatchResult.NeedMore();

        var length = newline + 1;
        var commandStr = System.Text.Encoding.ASCII.GetString(buffer[..length]);
        var commandRaw = Convert.ToHexString(buffer[..length]);

        // Extract and unescape text between quotes first
        var quoteStart = commandStr.IndexOf('"');
        if (quoteStart < 0)
            return MatchResult.Matched(new PrinterError("Missing opening quote in A text command"));

        var quoteEnd = EplStringHelpers.FindClosingQuote(commandStr, quoteStart + 1);
        if (quoteEnd < 0)
            return MatchResult.Matched(new PrinterError("Missing closing quote in A text command"));

        var escapedText = commandStr[(quoteStart + 1)..quoteEnd];
        var text = EplStringHelpers.Unescape(escapedText);

        // Parse comma-separated args before the quote
        var argsContent = commandStr[1..quoteStart]; // Skip 'A' and get content before quote
        var parts = argsContent.Split(',');

        if (parts.Length < 7)
            return MatchResult.Matched(new PrinterError($"Invalid A text parameters: expected at least 7 parts, got {parts.Length}"));

        try
        {
            var parser = new ArgsParser(parts, "A text");

            var x = parser.GetInt(0, "x");
            var y = parser.GetInt(1, "y");
            var rotation = parser.GetInt(2, "rotation");
            var font = parser.GetInt(3, "font");
            var hMul = parser.GetInt(4, "h-multiplication");
            var vMul = parser.GetInt(5, "v-multiplication");
            var reverse = parser.GetChar(6, 'N');

            var element = new ScalableText(x, y, rotation, font, hMul, vMul, reverse, text);
            return EplParsingHelpers.Success(element, commandRaw, length);
        }
        catch (ParseException ex)
        {
            return MatchResult.Matched(new PrinterError($"Invalid A text: {ex.Message}"));
        }
    }
}

/// <summary>
/// Command: LO x, y, thickness, length - Draw line (typically for underline).
/// ASCII: LO {x},{y},{thickness},{length}
/// HEX: 4C 4F {x},{y},{thickness},{length}
/// </summary>
public sealed class DrawHorizontalLineDescriptor : EplCommandDescriptor
{
    private const int FixedLength = 3; // 'L' + 'O'

    public override ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x4C, 0x4F }; // 'LO'
    public override int MinLength => FixedLength;

    public override MatchResult TryParse(ReadOnlySpan<byte> buffer, EplParserState state)
    {
        if (!EplParsingHelpers.TryFindNewlineFromEnd(buffer, out var newline))
            return MatchResult.NeedMore();

        var length = newline + 1;
        var commandRaw = Convert.ToHexString(buffer[..length]);

        return EplParsingHelpers.ParseCommaSeparatedArgs(
            System.Text.Encoding.ASCII.GetString(buffer[2..length]),
            "LO draw line",
            p =>
            {
                var x = p.GetInt(0, "x");
                var y = p.GetInt(1, "y");
                var thickness = p.GetInt(2, "thickness");
                var lineLength = p.GetInt(3, "length");
                return new DrawHorizontalLine(x, y, thickness, lineLength);
            }).WithMetadata(commandRaw, length);
    }
}

/// <summary>
/// Command: B x, y, rotation, type, width, height, hri, "data" - Barcode.
/// ASCII: B {x},{y},{rotation},{type},{width},{height},{hri},"{data}"
/// HEX: 42 {x},{y},{rotation},{type},{width},{height},{hri},{data}
/// </summary>
public sealed class PrintBarcodeDescriptor : EplCommandDescriptor
{
    private const int MinLen = 10;

    public override ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x42 }; // 'B'
    public override int MinLength => MinLen;

    public override MatchResult TryParse(ReadOnlySpan<byte> buffer, EplParserState state)
    {
        if (!EplParsingHelpers.TryFindNewlineFromEnd(buffer, out var newline))
            return MatchResult.NeedMore();

        var length = newline + 1;
        var commandStr = System.Text.Encoding.ASCII.GetString(buffer[..length]);
        var commandRaw = Convert.ToHexString(buffer[..length]);

        // Extract and unescape data between quotes first
        var quoteStart = commandStr.IndexOf('"');
        if (quoteStart < 0)
            return MatchResult.Matched(new PrinterError("Missing opening quote in B barcode command"));

        var quoteEnd = EplStringHelpers.FindClosingQuote(commandStr, quoteStart + 1);
        if (quoteEnd < 0)
            return MatchResult.Matched(new PrinterError("Missing closing quote in B barcode command"));

        var escapedData = commandStr[(quoteStart + 1)..quoteEnd];
        var data = EplStringHelpers.Unescape(escapedData);

        // Parse comma-separated args before the quote
        var argsContent = commandStr[1..quoteStart]; // Skip 'B' and get content before quote
        var parts = argsContent.Split(',');

        if (parts.Length < 7)
            return MatchResult.Matched(new PrinterError($"Invalid B barcode parameters: expected at least 7 parts, got {parts.Length}"));

        try
        {
            var parser = new ArgsParser(parts, "B barcode");

            var x = parser.GetInt(0, "x");
            var y = parser.GetInt(1, "y");
            var rotation = parser.GetInt(2, "rotation");
            var type = parser.GetString(3);
            var width = parser.GetInt(4, "width");
            var height = parser.GetInt(5, "height");
            var hri = parser.GetChar(6, 'N');

            var element = new PrintBarcode(x, y, rotation, type, width, height, hri, data);
            return EplParsingHelpers.Success(element, commandRaw, length);
        }
        catch (ParseException ex)
        {
            return MatchResult.Matched(new PrinterError($"Invalid B barcode: {ex.Message}"));
        }
    }
}

/// <summary>
/// Command: X x1, y1, thickness, x2, y2 - Draw line/box.
/// ASCII: X {x1},{y1},{thickness},{x2},{y2}
/// HEX: 58 {x1},{y1},{thickness},{x2},{y2}
/// </summary>
public sealed class DrawLineDescriptor : EplCommandDescriptor
{
    private const int MinLen = 5;

    public override ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x58 }; // 'X'
    public override int MinLength => MinLen;

    public override MatchResult TryParse(ReadOnlySpan<byte> buffer, EplParserState state)
    {
        if (!EplParsingHelpers.TryFindNewlineFromEnd(buffer, out var newline))
            return MatchResult.NeedMore();

        var length = newline + 1;
        var commandRaw = Convert.ToHexString(buffer[..length]);

        return EplParsingHelpers.ParseCommaSeparatedArgs(
            System.Text.Encoding.ASCII.GetString(buffer[1..length]),
            "X draw line",
            p =>
            {
                var x1 = p.GetInt(0, "x1");
                var y1 = p.GetInt(1, "y1");
                var thickness = p.GetInt(2, "thickness");
                var x2 = p.GetInt(3, "x2");
                var y2 = p.GetInt(4, "y2");
                return new DrawLine(x1, y1, thickness, x2, y2);
            }).WithMetadata(commandRaw, length);
    }
}

/// <summary>
/// Command: P n - Print format and feed.
/// ASCII: P {n}
/// HEX: 50 {n}
/// </summary>
public sealed class PrintDescriptor : EplCommandDescriptor
{
    private const int MinLen = 2;

    public override ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x50 }; // 'P'
    public override int MinLength => MinLen;

    public override MatchResult TryParse(ReadOnlySpan<byte> buffer, EplParserState state)
    {
        if (!EplParsingHelpers.TryFindNewlineFromEnd(buffer, out var newline))
            return MatchResult.NeedMore();

        var length = newline + 1;
        if (length < 2)
            return MatchResult.Matched(new PrinterError("Invalid P print: too short"));

        var parseResult = EplParsingHelpers.ParseSingleIntArg(buffer, 1, "P print", out var copies);
        if (parseResult.HasValue)
            return parseResult.Value;

        var element = new Print(copies);
        return EplParsingHelpers.Success(element, buffer, length);
    }
}
