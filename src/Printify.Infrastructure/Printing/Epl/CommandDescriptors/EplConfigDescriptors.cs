using Printify.Domain.Documents.Elements;
using Printify.Domain.Documents.Elements.Epl;
using Printify.Infrastructure.Printing.Common;

namespace Printify.Infrastructure.Printing.Epl.CommandDescriptors;

/// <summary>
/// Command: q width - Set label width.
/// ASCII: q {width}
/// HEX: 71 {width}
/// </summary>
public sealed class SetLabelWidthDescriptor : EplCommandDescriptor
{
    private const int MinLen = 2; // 'q' + at least 1 digit

    public override ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x71 }; // 'q'
    public override int MinLength => MinLen;

    public override MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        if (!EplParsingHelpers.TryFindNewlineFromEnd(buffer, out var newline))
            return MatchResult.NeedMore();

        var length = newline + 1;
        if (length < 2)
            return MatchResult.Matched(new PrinterError("Invalid q width: too short"));

        var parseResult = EplParsingHelpers.ParseSingleIntArg(buffer, 1, "q width", out var width);
        if (parseResult.HasValue)
            return parseResult.Value;

        var element = new SetLabelWidth(width);
        return EplParsingHelpers.Success(element, buffer, length);
    }
}

/// <summary>
/// Command: Q height, [param2] - Set label height.
/// ASCII: Q {height},{param2}
/// HEX: 51 {height},{param2}
/// </summary>
public sealed class SetLabelHeightDescriptor : EplCommandDescriptor
{
    private const int MinLen = 3; // 'Q' + at least 1 digit + comma

    public override ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x51 }; // 'Q'
    public override int MinLength => MinLen;

    public override MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        if (!EplParsingHelpers.TryFindNewlineFromEnd(buffer, out var newline))
            return MatchResult.NeedMore();

        var length = newline + 1;
        var commandRaw = Convert.ToHexString(buffer[..length]);

        return EplParsingHelpers.ParseCommaSeparatedArgs(
            System.Text.Encoding.ASCII.GetString(buffer[1..length]),
            "Q height",
            p =>
            {
                var height = p.GetInt(0, "height");
                var param2 = p.GetIntOrDefault(1, 0);

                return new SetLabelHeight(height, param2);
            }).WithMetadata(commandRaw, length);
    }
}

/// <summary>
/// Command: R speed - Set print speed.
/// ASCII: R {speed}
/// HEX: 52 {speed}
/// </summary>
public sealed class SetPrintSpeedDescriptor : EplCommandDescriptor
{
    private const int MinLen = 2;

    public override ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x52 }; // 'R'
    public override int MinLength => MinLen;

    public override MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        if (!EplParsingHelpers.TryFindNewlineFromEnd(buffer, out var newline))
            return MatchResult.NeedMore();

        var length = newline + 1;
        if (length < 2)
            return MatchResult.Matched(new PrinterError("Invalid R speed: too short"));

        var parseResult = EplParsingHelpers.ParseSingleIntArg(buffer, 1, "R speed", out var speed);
        if (parseResult.HasValue)
            return parseResult.Value;

        var element = new SetPrintSpeed(speed);
        return EplParsingHelpers.Success(element, buffer, length);
    }
}

/// <summary>
/// Command: S darkness - Set print darkness.
/// ASCII: S {darkness}
/// HEX: 53 {darkness}
/// </summary>
public sealed class SetPrintDarknessDescriptor : EplCommandDescriptor
{
    private const int MinLen = 2;

    public override ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x53 }; // 'S'
    public override int MinLength => MinLen;

    public override MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        if (!EplParsingHelpers.TryFindNewlineFromEnd(buffer, out var newline))
            return MatchResult.NeedMore();

        var length = newline + 1;
        if (length < 2)
            return MatchResult.Matched(new PrinterError("Invalid S darkness: too short"));

        var parseResult = EplParsingHelpers.ParseSingleIntArg(buffer, 1, "S darkness", out var darkness);
        if (parseResult.HasValue)
            return parseResult.Value;

        var element = new SetPrintDarkness(darkness);
        return EplParsingHelpers.Success(element, buffer, length);
    }
}

/// <summary>
/// Command: Z direction - Set print direction.
/// ASCII: Z T or Z B
/// HEX: 5A 54 or 5A 42
/// </summary>
public sealed class SetPrintDirectionDescriptor : EplCommandDescriptor
{
    private const int FixedLength = 3; // 'Z' + 'T'/'B' + newline

    public override ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x5A }; // 'Z'
    public override int MinLength => FixedLength;

    public override MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        const int length = 3;

        if (buffer.Length < length)
            return MatchResult.NeedMore();

        if (buffer[2] != (byte)'\n')
            return MatchResult.NeedMore();

        var direction = (char)buffer[1];
        if (direction is 'T' or 'B' or 't' or 'b')
        {
            var element = new SetPrintDirection(char.ToUpper(direction));
            return EplParsingHelpers.Success(element, buffer, length);
        }

        return MatchResult.Matched(new PrinterError($"Invalid Z direction: '{direction}' (expected 'T' or 'B')"));
    }
}

/// <summary>
/// Command: I code - Set international character set.
/// ASCII: I {code}
/// HEX: 49 {code}
/// </summary>
public sealed class SetInternationalCharacterDescriptor : EplCommandDescriptor
{
    private const int MinLen = 2;

    public override ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x49 }; // 'I'
    public override int MinLength => MinLen;

    public override MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        if (!EplParsingHelpers.TryFindNewlineFromEnd(buffer, out var newline))
            return MatchResult.NeedMore();

        var length = newline + 1;
        if (length < 2)
            return MatchResult.Matched(new PrinterError("Invalid I code: too short"));

        var parseResult = EplParsingHelpers.ParseSingleIntArg(buffer, 1, "I code", out var code);
        if (parseResult.HasValue)
            return parseResult.Value;

        // Note: Encoding updates are now handled in EplParser.ModifyDeviceContext()
        var element = new SetInternationalCharacter(code);
        return EplParsingHelpers.Success(element, buffer, length);
    }
}

/// <summary>
/// Command: i code, scaling - Set codepage.
/// ASCII: i {code},{scaling}
/// HEX: 69 {code},{scaling}
/// </summary>
public sealed class SetCodePageDescriptor : EplCommandDescriptor
{
    private const int MinLen = 2;

    public override ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x69 }; // 'i'
    public override int MinLength => MinLen;

    public override MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        if (!EplParsingHelpers.TryFindNewlineFromEnd(buffer, out var newline))
            return MatchResult.NeedMore();

        var length = newline + 1;
        var commandRaw = Convert.ToHexString(buffer[..length]);

        return EplParsingHelpers.ParseCommaSeparatedArgs(
            System.Text.Encoding.ASCII.GetString(buffer[1..length]),
            "i codepage",
            p =>
            {
                var code = p.GetInt(0, "code");
                var scaling = p.GetIntOrDefault(1, 0);

                // Note: Encoding updates are now handled in EplParser.ModifyDeviceContext()
                return new SetCodePage(code, scaling);
            }).WithMetadata(commandRaw, length);
    }
}

/// <summary>
/// Extension helper to add metadata to MatchResult.
/// </summary>
internal static class MatchResultExtensions
{
    public static MatchResult WithMetadata(this MatchResult result, string commandRaw, int length)
    {
        if (result.Kind == MatchKind.Matched && result.Element is Element element)
        {
            return EplParsingHelpers.Success(element, commandRaw, length);
        }
        return result;
    }
}
