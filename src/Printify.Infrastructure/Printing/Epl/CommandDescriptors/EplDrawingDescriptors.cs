using Printify.Domain.Documents.Elements;
using Printify.Domain.Documents.Elements.Epl;
using Printify.Infrastructure.Printing.Common;
using System.Globalization;

namespace Printify.Infrastructure.Printing.Epl.CommandDescriptors;

/// Command: A x, y, rotation, font, h-mul, v-mul, reverse, "text" - Scalable/rotatable text.
/// ASCII: A {x},{y},{rotation},{font},{h},{v},{reverse},"{text}"
/// HEX: 41 {x},{y},{rotation},{font},{h},{v},{reverse},{text}
public sealed class EplA2TextDescriptor : ICommandDescriptor<EplParserState>
{
    private const int MinLen = 10; // 'A' + minimum params

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x41 }; // 'A'
    public int MinLength => MinLen;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer)
    {
        // Find closing quote and newline
        var closeQuote = buffer.IndexOf((byte)'"');
        if (closeQuote < 0)
            return null;

        var newline = buffer[closeQuote..].IndexOf((byte)'\n');
        if (newline < 0)
            return null;

        return closeQuote + newline + 1;
    }

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, EplParserState state)
    {
        var closeQuote = buffer.IndexOf((byte)'"');
        if (closeQuote < 0)
            return MatchResult.NeedMore();

        var afterQuote = closeQuote + 1;
        var totalLen = afterQuote;
        if (buffer.Length > afterQuote && buffer[afterQuote] == (byte)'\\')
            totalLen += 1; // Skip escaped quote
        if (buffer.Length > totalLen && buffer[totalLen] == (byte)'\n')
            totalLen += 1;

        // Parse: A{x},{y},{rotation},{font},{h},{v},{reverse},"{text}"
        var headerStr = System.Text.Encoding.ASCII.GetString(buffer[..Math.Min(closeQuote, 100)]);
        var headerParts = headerStr.Split(',');

        if (headerParts.Length < 7)
            return MatchResult.NeedMore();

        if (!int.TryParse(headerParts[0].TrimStart('A'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ||
            !int.TryParse(headerParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y) ||
            !int.TryParse(headerParts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var rotation) ||
            !int.TryParse(headerParts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var font) ||
            !int.TryParse(headerParts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hMul) ||
            !int.TryParse(headerParts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var vMul))
        {
            var error = new PrinterError($"Invalid A2 text parameters");
            return MatchResult.Matched(error);
        }

        var reverseStr = headerParts[6].Trim();
        var reverse = reverseStr.Length > 0 ? reverseStr[0] : 'N';

        // Extract text between quotes
        var textStart = headerStr.IndexOf('"') + 1;
        var textEnd = headerStr.IndexOf('"', textStart);
        var text = textEnd > textStart ? headerStr[textStart..textEnd] : "";

        var element = new ScalableText(x, y, rotation, font, hMul, vMul, reverse, text)
        {
            CommandRaw = Convert.ToHexString(buffer[..Math.Min(totalLen, buffer.Length)]),
            LengthInBytes = Math.Min(totalLen, buffer.Length)
        };
        return MatchResult.Matched(element);
    }
}

/// Command: LO x, y, thickness, length - Draw line (typically for underline).
/// ASCII: LO {x},{y},{thickness},{length}
/// HEX: 4C 4F {x},{y},{thickness},{length}
public sealed class EplLODrawLineDescriptor : ICommandDescriptor<EplParserState>
{
    private const int FixedLength = 3; // 'L' + 'O'

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x4C, 0x4F }; // 'LO'
    public int MinLength => FixedLength;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer)
    {
        var end = buffer.IndexOf((byte)'\n');
        return end >= 0 ? end + 1 : null;
    }

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, EplParserState state)
    {
        var end = buffer.IndexOf((byte)'\n');
        if (end < 0)
            return MatchResult.NeedMore();

        var length = end + 1;

        // Parse: LO{x},{y},{thickness},{length}
        var content = System.Text.Encoding.ASCII.GetString(buffer[2..length]);
        var parts = content.TrimEnd('\n').Split(',');

        if (parts.Length >= 4 &&
            int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) &&
            int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var y) &&
            int.TryParse(parts[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var thickness) &&
            int.TryParse(parts[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var lineLength))
        {
            var element = new DrawHorizontalLine(x, y, thickness, lineLength)
            {
                CommandRaw = Convert.ToHexString(buffer[..length]),
                LengthInBytes = length
            };
            return MatchResult.Matched(element);
        }

        var error = new PrinterError($"Invalid LO draw line parameters");
        return MatchResult.Matched(error);
    }
}

/// Command: B x, y, rotation, type, width, height, hri, "data" - Barcode.
/// ASCII: B {x},{y},{rotation},{type},{width},{height},{hri},"{data}"
/// HEX: 42 {x},{y},{rotation},{type},{width},{height},{hri},{data}
public sealed class EplBBarcodeDescriptor : ICommandDescriptor<EplParserState>
{
    private const int MinLen = 10;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x42 }; // 'B'
    public int MinLength => MinLen;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer)
    {
        var closeQuote = buffer.IndexOf((byte)'"');
        if (closeQuote < 0)
            return null;

        var newline = buffer[closeQuote..].IndexOf((byte)'\n');
        if (newline < 0)
            return null;

        return closeQuote + newline + 1;
    }

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, EplParserState state)
    {
        var closeQuote = buffer.IndexOf((byte)'"');
        if (closeQuote < 0)
            return MatchResult.NeedMore();

        var afterQuote = closeQuote + 1;
        var totalLen = buffer.Length > afterQuote && buffer[afterQuote] == (byte)'\n' ? afterQuote + 1 : afterQuote;

        // Parse: B{x},{y},{rotation},{type},{width},{height},{hri},"{data}"
        var headerStr = System.Text.Encoding.ASCII.GetString(buffer[..Math.Min(closeQuote, 200)]);
        var headerParts = headerStr.Split(',');

        if (headerParts.Length < 7)
            return MatchResult.NeedMore();

        if (!int.TryParse(headerParts[0].TrimStart('B'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ||
            !int.TryParse(headerParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y) ||
            !int.TryParse(headerParts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var rotation) ||
            !int.TryParse(headerParts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var width) ||
            !int.TryParse(headerParts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var height))
        {
            var error = new PrinterError($"Invalid B barcode parameters");
            return MatchResult.Matched(error);
        }

        var type = headerParts[3].Trim();
        var hriStr = headerParts[6].Trim();
        var hri = hriStr.Length > 0 ? hriStr[0] : 'N';

        // Extract text between quotes
        var textStart = headerStr.IndexOf('"') + 1;
        var textEnd = headerStr.IndexOf('"', textStart);
        var data = textEnd > textStart ? headerStr[textStart..textEnd] : "";

        var element = new PrintBarcode(x, y, rotation, type, width, height, hri, data)
        {
            CommandRaw = Convert.ToHexString(buffer[..Math.Min(totalLen, buffer.Length)]),
            LengthInBytes = Math.Min(totalLen, buffer.Length)
        };
        return MatchResult.Matched(element);
    }
}

/// Command: X x1, y1, thickness, x2, y2 - Draw line/box.
/// ASCII: X {x1},{y1},{thickness},{x2},{y2}
/// HEX: 58 {x1},{y1},{thickness},{x2},{y2}
public sealed class EplXDrawLineDescriptor : ICommandDescriptor<EplParserState>
{
    private const int MinLen = 5;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x58 }; // 'X'
    public int MinLength => MinLen;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer)
    {
        var end = buffer.IndexOf((byte)'\n');
        return end >= 0 ? end + 1 : null;
    }

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, EplParserState state)
    {
        var end = buffer.IndexOf((byte)'\n');
        if (end < 0)
            return MatchResult.NeedMore();

        var length = end + 1;

        // Parse: X{x1},{y1},{thickness},{x2},{y2}
        var content = System.Text.Encoding.ASCII.GetString(buffer[1..length]);
        var parts = content.TrimEnd('\n').Split(',');

        if (parts.Length >= 5 &&
            int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var x1) &&
            int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var y1) &&
            int.TryParse(parts[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var thickness) &&
            int.TryParse(parts[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var x2) &&
            int.TryParse(parts[4].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var y2))
        {
            var element = new DrawLine(x1, y1, thickness, x2, y2)
            {
                CommandRaw = Convert.ToHexString(buffer[..length]),
                LengthInBytes = length
            };
            return MatchResult.Matched(element);
        }

        var error = new PrinterError($"Invalid X draw line parameters");
        return MatchResult.Matched(error);
    }
}

/// Command: P n - Print format and feed.
/// ASCII: P {n}
/// HEX: 50 {n}
public sealed class EplPfPrintAndFeedDescriptor : ICommandDescriptor<EplParserState>
{
    private const int MinLen = 2;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x50 }; // 'P'
    public int MinLength => MinLen;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer)
    {
        var end = buffer.IndexOf((byte)'\n');
        return end >= 0 ? end + 1 : null;
    }

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, EplParserState state)
    {
        var end = buffer.IndexOf((byte)'\n');
        if (end < 0)
            return MatchResult.NeedMore();

        var length = end + 1;

        if (length < 2)
            return MatchResult.Matched(new PrinterError("Invalid P print: too short"));

        var copiesStr = System.Text.Encoding.ASCII.GetString(buffer[1..length]);
        if (int.TryParse(copiesStr.TrimEnd('\n'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var copies))
        {
            var element = new Print(copies)
            {
                CommandRaw = Convert.ToHexString(buffer[..length]),
                LengthInBytes = length
            };
            return MatchResult.Matched(element);
        }

        var error = new PrinterError($"Invalid P print copies format");
        return MatchResult.Matched(error);
    }
}
