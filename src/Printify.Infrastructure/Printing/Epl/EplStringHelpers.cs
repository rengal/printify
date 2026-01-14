namespace Printify.Infrastructure.Printing.Epl;

/// <summary>
/// Helper methods for parsing EPL string values.
/// </summary>
public static class EplStringHelpers
{
    /// <summary>
    /// Unescapes an EPL-escaped string.
    /// EPL escape sequences:
    /// - \" -> "
    /// - \\ -> \
    /// - \n -> newline (LF)
    /// - \r -> carriage return (CR)
    /// - \t -> tab
    /// - \xNN -> hex byte value
    /// </summary>
    /// <param name="escaped">The escaped string.</param>
    /// <returns>The unescaped string.</returns>
    public static string Unescape(string escaped)
    {
        if (string.IsNullOrEmpty(escaped))
            return escaped;

        var result = new System.Text.StringBuilder(escaped.Length);
        var i = 0;

        while (i < escaped.Length)
        {
            if (escaped[i] == '\\' && i + 1 < escaped.Length)
            {
                var nextChar = escaped[i + 1];
                switch (nextChar)
                {
                    case '"':
                        result.Append('"');
                        i += 2;
                        break;
                    case '\\':
                        result.Append('\\');
                        i += 2;
                        break;
                    case 'n':
                    case 'N':
                        result.Append('\n');
                        i += 2;
                        break;
                    case 'r':
                    case 'R':
                        result.Append('\r');
                        i += 2;
                        break;
                    case 't':
                    case 'T':
                        result.Append('\t');
                        i += 2;
                        break;
                    case 'x':
                    case 'X':
                        // Hex escape sequence: \xNN
                        if (i + 3 < escaped.Length)
                        {
                            var hexStr = escaped.Substring(i + 2, 2);
                            if (byte.TryParse(hexStr, System.Globalization.NumberStyles.HexNumber, null, out var hexValue))
                            {
                                result.Append((char)hexValue);
                                i += 4;
                            }
                            else
                            {
                                // Invalid hex, treat as literal
                                result.Append(escaped[i]);
                                i++;
                            }
                        }
                        else
                        {
                            // Not enough chars for hex, treat as literal
                            result.Append(escaped[i]);
                            i++;
                        }
                        break;
                    default:
                        // Unknown escape, treat as literal
                        result.Append(escaped[i]);
                        i++;
                        break;
                }
            }
            else
            {
                result.Append(escaped[i]);
                i++;
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Finds the end of a quoted string in an EPL command, handling escape sequences.
    /// </summary>
    /// <param name="str">The string to search.</param>
    /// <param name="startIndex">The index to start searching from (should be after the opening quote).</param>
    /// <returns>The index of the closing quote, or -1 if not found.</returns>
    public static int FindClosingQuote(string str, int startIndex)
    {
        var i = startIndex;
        while (i < str.Length)
        {
            if (str[i] == '\\' && i + 1 < str.Length)
            {
                // Skip the escape character and the escaped character
                i += 2;
            }
            else if (str[i] == '"')
            {
                return i;
            }
            else
            {
                i++;
            }
        }
        return -1;
    }
}
