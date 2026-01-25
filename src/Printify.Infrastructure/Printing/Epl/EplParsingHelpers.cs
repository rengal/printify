using Printify.Domain.Printing;
using Printify.Infrastructure.Printing.Common;
using System.Globalization;

namespace Printify.Infrastructure.Printing.Epl;

/// <summary>
/// Common helper methods for EPL command parsing.
/// </summary>
public static class EplParsingHelpers
{
    /// <summary>
    /// Finds the line terminator (CR or LF) in the buffer by searching from beginning to end.
    /// This is stable for streaming scenarios - as buffer grows, we scan forward and find the first terminator.
    /// Accepts either CR (0x0D) or LF (0x0A) as command terminator.
    /// </summary>
    /// <param name="buffer">The buffer to search.</param>
    /// <param name="terminatorIndex">Outputs the index of the terminator character.</param>
    /// <returns>True if terminator was found, false otherwise.</returns>
    public static bool TryFindTerminator(ReadOnlySpan<byte> buffer, out int terminatorIndex)
    {
        // Search forward from beginning - this is stable for streaming
        // As buffer grows, we find the first terminator at its position
        for (int i = 0; i < buffer.Length; i++)
        {
            var b = buffer[i];
            if (b == 0x0A || b == 0x0D) // LF or CR
            {
                terminatorIndex = i;
                return true;
            }
        }
        terminatorIndex = -1;
        return false;
    }

    /// <summary>
    /// Parses comma-separated arguments from a string with type-safe conversion.
    /// Returns a MatchResult with either a PrinterError element or the parsed result.
    /// </summary>
    public static MatchResult ParseCommaSeparatedArgs<T>(
        string content,
        string commandName,
        Func<ArgsParser, T> resultFactory) where T : Command
    {
        var trimmed = content.TrimEnd('\n').TrimEnd('\r');
        var parts = trimmed.Split(',');

        try
        {
            var parser = new ArgsParser(parts, commandName);
            var element = resultFactory(parser);
            return MatchResult.Matched(element);
        }
        catch (ParseException ex)
        {
            return MatchResult.Matched(new PrinterError($"Invalid {commandName}: {ex.Message}"));
        }
    }

    /// <summary>
    /// Parses comma-separated arguments from a string with type-safe conversion and custom result.
    /// Returns a MatchResult with either a PrinterError element or the parsed result.
    /// </summary>
    public static MatchResult ParseCommaSeparatedArgs(
        string content,
        string commandName,
        Func<ArgsParser, Command> resultFactory)
    {
        var trimmed = content.TrimEnd('\n').TrimEnd('\r');
        var parts = trimmed.Split(',');

        try
        {
            var parser = new ArgsParser(parts, commandName);
            var element = resultFactory(parser);
            return MatchResult.Matched(element);
        }
        catch (ParseException ex)
        {
            return MatchResult.Matched(new PrinterError($"Invalid {commandName}: {ex.Message}"));
        }
    }

    /// <summary>
    /// Parses a single integer value from content after the command prefix.
    /// Returns a MatchResult with error if failed, or null if successful.
    /// </summary>
    public static MatchResult? ParseSingleIntArg(
        ReadOnlySpan<byte> buffer,
        int commandLength,
        string commandName,
        out int value)
    {
        value = 0;
        var content = System.Text.Encoding.ASCII.GetString(buffer[commandLength..]);
        var trimmed = content.TrimEnd('\n').TrimEnd('\r');

        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            value = parsed;
            return null; // Success
        }

        return MatchResult.Matched(new PrinterError($"Invalid {commandName}: '{trimmed}' is not a valid integer"));
    }

    /// <summary>
    /// Creates a MatchResult for a successfully parsed element with command metadata.
    /// Note: This requires the element to have RawBytes and LengthInBytes as init-only properties.
    /// </summary>
    public static MatchResult Success(Command element, ReadOnlySpan<byte> buffer, int length)
    {
        // Create a new element with the properties set via reflection (for init-only properties)
        // This is needed because RawBytes and LengthInBytes are init-only
        var elementType = element.GetType();
        var rawBytesProperty = elementType.GetProperty(nameof(Command.RawBytes));
        var lengthInBytesProperty = elementType.GetProperty(nameof(Command.LengthInBytes));

        if (rawBytesProperty != null && rawBytesProperty.CanWrite)
            rawBytesProperty.SetValue(element, buffer[..length].ToArray());

        if (lengthInBytesProperty != null && lengthInBytesProperty.CanWrite)
            lengthInBytesProperty.SetValue(element, length);
        else if (lengthInBytesProperty?.SetMethod != null)
            // For init-only properties, we need to use the setter via reflection
            lengthInBytesProperty.SetMethod.Invoke(element, [length]);

        return MatchResult.Matched(element);
    }
}

/// <summary>
/// Exception thrown during parsing when a parameter value is invalid.
/// </summary>
public sealed class ParseException : Exception
{
    public ParseException(string message) : base(message) { }
}

/// <summary>
/// Helper for parsing typed arguments from string parts.
/// </summary>
public sealed class ArgsParser
{
    private readonly string[] _parts;

    public ArgsParser(string[] parts, string commandName)
    {
        _parts = parts;
    }

    /// <summary>
    /// Gets the number of parts available.
    /// </summary>
    public int Count => _parts.Length;

    /// <summary>
    /// Ensures there are at least the specified number of parts.
    /// </summary>
    public void RequireAtLeast(int count)
    {
        if (_parts.Length < count)
            throw new ParseException($"expected at least {count} parameters, got {_parts.Length}");
    }

    /// <summary>
    /// Parses an integer at the specified index.
    /// </summary>
    public int GetInt(int index, string? paramName = null)
    {
        if (index >= _parts.Length)
            throw new ParseException($"missing parameter at index {index}");

        if (int.TryParse(_parts[index].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            return result;

        throw new ParseException($"parameter '{paramName ?? index.ToString()}' ('{_parts[index]}') is not a valid integer");
    }

    /// <summary>
    /// Parses an optional integer at the specified index, returning defaultValue if not present or invalid.
    /// </summary>
    public int GetIntOrDefault(int index, int defaultValue)
    {
        if (index >= _parts.Length)
            return defaultValue;

        if (int.TryParse(_parts[index].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            return result;

        return defaultValue;
    }

    /// <summary>
    /// Gets a string at the specified index.
    /// </summary>
    public string GetString(int index)
    {
        if (index >= _parts.Length)
            throw new ParseException($"missing parameter at index {index}");

        return _parts[index].Trim();
    }

    /// <summary>
    /// Gets a string at the specified index, or defaultValue if not present.
    /// </summary>
    public string? GetStringOrDefault(int index, string? defaultValue = null)
    {
        if (index >= _parts.Length)
            return defaultValue;

        return _parts[index].Trim();
    }

    /// <summary>
    /// Gets a character at the specified index (first char of the string).
    /// </summary>
    public char GetChar(int index, char defaultValue = '\0')
    {
        var str = GetStringOrDefault(index);
        return string.IsNullOrEmpty(str) ? defaultValue : str[0];
    }
}
