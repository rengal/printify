using Printify.Domain.Documents.Elements;
using Printify.Domain.Documents.Elements.Epl;
using Printify.Infrastructure.Printing.Common;
using System.Globalization;

namespace Printify.Infrastructure.Printing.Epl.CommandDescriptors;

/// Command: q width - Set label width.
/// ASCII: q {width}
/// HEX: 71 {width}
public sealed class EplqWidthDescriptor : ICommandDescriptor<EplParserState>
{
    private const int MinLen = 2; // 'q' + at least 1 digit

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x71 }; // 'q'
    public int MinLength => MinLen;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer)
    {
        // Find end of command (newline or end of buffer)
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
            return MatchResult.Matched(new PrinterError("Invalid q width: too short"));

        // Parse width number after 'q'
        var widthStr = System.Text.Encoding.ASCII.GetString(buffer[1..length]);
        if (int.TryParse(widthStr.TrimEnd('\n'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var width))
        {
            var element = new SetLabelWidth(width)
            {
                CommandRaw = Convert.ToHexString(buffer[..length]),
                LengthInBytes = length
            };
            state.LabelWidth = width;
            return MatchResult.Matched(element);
        }

        var error = new PrinterError($"Invalid q width format");
        return MatchResult.Matched(error);
    }
}

/// Command: Q height, [param2] - Set label height.
/// ASCII: Q {height},{param2}
/// HEX: 51 {height},{param2}
public sealed class EplQHeightDescriptor : ICommandDescriptor<EplParserState>
{
    private const int MinLen = 3; // 'Q' + at least 1 digit + comma

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x51 }; // 'Q'
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

        // Parse: Q{height},{param2}
        var content = System.Text.Encoding.ASCII.GetString(buffer[1..length]);
        var parts = content.TrimEnd('\n').Split(',');

        if (parts.Length >= 1 && int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var height))
        {
            var param2 = parts.Length > 1 && int.TryParse(parts[1].Trim(), out var p2) ? p2 : 0;
            var element = new SetLabelHeight(height, param2)
            {
                CommandRaw = Convert.ToHexString(buffer[..length]),
                LengthInBytes = length
            };
            state.LabelHeight = height;
            return MatchResult.Matched(element);
        }

        var error = new PrinterError($"Invalid Q height format");
        return MatchResult.Matched(error);
    }
}

/// Command: R speed - Set print speed.
/// ASCII: R {speed}
/// HEX: 52 {speed}
public sealed class EplRSpeedDescriptor : ICommandDescriptor<EplParserState>
{
    private const int MinLen = 2;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x52 }; // 'R'
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
            return MatchResult.Matched(new PrinterError("Invalid R speed: too short"));

        var speedStr = System.Text.Encoding.ASCII.GetString(buffer[1..length]);
        if (int.TryParse(speedStr.TrimEnd('\n'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var speed))
        {
            var element = new SetPrintSpeed(speed)
            {
                CommandRaw = Convert.ToHexString(buffer[..length]),
                LengthInBytes = length
            };
            state.PrintSpeed = speed;
            return MatchResult.Matched(element);
        }

        var error = new PrinterError($"Invalid R speed format");
        return MatchResult.Matched(error);
    }
}

/// Command: S darkness - Set print darkness.
/// ASCII: S {darkness}
/// HEX: 53 {darkness}
public sealed class EplSDarknessDescriptor : ICommandDescriptor<EplParserState>
{
    private const int MinLen = 2;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x53 }; // 'S'
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
            return MatchResult.Matched(new PrinterError("Invalid S darkness: too short"));

        var darknessStr = System.Text.Encoding.ASCII.GetString(buffer[1..length]);
        if (int.TryParse(darknessStr.TrimEnd('\n'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var darkness))
        {
            var element = new SetPrintDarkness(darkness)
            {
                CommandRaw = Convert.ToHexString(buffer[..length]),
                LengthInBytes = length
            };
            state.PrintDarkness = darkness;
            return MatchResult.Matched(element);
        }

        var error = new PrinterError($"Invalid S darkness format");
        return MatchResult.Matched(error);
    }
}

/// Command: Z direction - Set print direction.
/// ASCII: Z T or Z B
/// HEX: 5A 54 or 5A 42
public sealed class EplZDirectionDescriptor : ICommandDescriptor<EplParserState>
{
    private const int FixedLength = 3; // 'Z' + 'T'/'B' + newline

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x5A }; // 'Z'
    public int MinLength => FixedLength;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => buffer.Length >= 3 ? 3 : null;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, EplParserState state)
    {
        if (buffer.Length < 3)
            return MatchResult.NeedMore();

        const int length = 3;
        var direction = (char)buffer[1];
        if (direction is 'T' or 'B' or 't' or 'b')
        {
            var element = new SetPrintDirection(char.ToUpper(direction))
            {
                CommandRaw = Convert.ToHexString(buffer[..length]),
                LengthInBytes = length
            };
            return MatchResult.Matched(element);
        }

        var error = new PrinterError($"Invalid Z direction: {direction}");
        return MatchResult.Matched(error);
    }
}

/// Command: I code - Set international character set.
/// ASCII: I {code}
/// HEX: 49 {code}
public sealed class EplIInternationalCharacterDescriptor : ICommandDescriptor<EplParserState>
{
    private const int MinLen = 2;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x49 }; // 'I'
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
            return MatchResult.Matched(new PrinterError("Invalid I code: too short"));

        var codeStr = System.Text.Encoding.ASCII.GetString(buffer[1..length]);
        if (int.TryParse(codeStr.TrimEnd('\n'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var code))
        {
            // Update encoding based on code (simplified mapping)
            if (code is 8 or 38) // DOS 866 Cyrillic
                state.Encoding = System.Text.Encoding.GetEncoding(866);

            var element = new SetInternationalCharacter(code)
            {
                CommandRaw = Convert.ToHexString(buffer[..length]),
                LengthInBytes = length
            };
            return MatchResult.Matched(element);
        }

        var error = new PrinterError($"Invalid I code format");
        return MatchResult.Matched(error);
    }
}

/// Command: i code, scaling - Set codepage.
/// ASCII: i {code},{scaling}
/// HEX: 69 {code},{scaling}
public sealed class EpliCodePageDescriptor : ICommandDescriptor<EplParserState>
{
    private const int MinLen = 2;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x69 }; // 'i'
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

        // Parse: i{code},{scaling}
        var content = System.Text.Encoding.ASCII.GetString(buffer[1..length]);
        var parts = content.TrimEnd('\n').Split(',');

        if (parts.Length >= 1 && int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var code))
        {
            var scaling = parts.Length > 1 && int.TryParse(parts[1].Trim(), out var s) ? s : 0;

            // Update encoding based on code (simplified mapping)
            if (code is 0 or 8) // DOS 866 Cyrillic
                state.Encoding = System.Text.Encoding.GetEncoding(866);

            var element = new SetCodePage(code, scaling)
            {
                CommandRaw = Convert.ToHexString(buffer[..length]),
                LengthInBytes = length
            };
            return MatchResult.Matched(element);
        }

        var error = new PrinterError($"Invalid i codepage format");
        return MatchResult.Matched(error);
    }
}
