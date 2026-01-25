using Printify.Infrastructure.Printing.Epl.Commands;
using Printify.Infrastructure.Printing.Common;
using System.Globalization;
using Printify.Domain.Printing;

namespace Printify.Infrastructure.Printing.Epl.CommandDescriptors;

/// <summary>
/// Command: GW x, y, bytesPerRow, height, - Graphic write.
/// ASCII: GW {x},{y},{bytesPerRow},{height},
/// HEX: 47 57 {x},{y},{bytesPerRow},{height},
/// Followed by binary graphics data (bytesPerRow * height bytes) and then newline.
/// </summary>
/// <remarks>
/// This descriptor is special - it has a useful TryGetExactLength implementation
/// because the command length can be calculated from the header parameters.
/// </remarks>
public sealed class PrintGraphicDescriptor : ICommandDescriptor
{
    private const int MinLen = 10; // 'GW' + minimum params

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x47, 0x57 }; // 'GW'
    public int MinLength => MinLen;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer)
    {
        // Find the comma at end of header
        var commaIndex = buffer.IndexOf((byte)',');
        if (commaIndex < 3) // Must be after "GW"
            return null;

        // Parse the header to get total data bytes
        var headerStr = System.Text.Encoding.ASCII.GetString(buffer[..commaIndex]);
        var parts = headerStr[2..].Split(','); // Skip "GW"

        if (parts.Length < 4)
            return null;

        if (!int.TryParse(parts[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var bytesPerRow) ||
            !int.TryParse(parts[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var height))
            return null;

        var totalDataBytes = bytesPerRow * height;
        var totalLength = commaIndex + 1 + totalDataBytes + 1; // +1 for comma, +1 for newline

        // Only return exact length if we have enough data
        return buffer.Length >= totalLength ? totalLength : null;
    }

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        // Find the comma at end of header
        var commaIndex = buffer.IndexOf((byte)',');
        if (commaIndex < 3)
            return MatchResult.NeedMore();

        // Parse header: GW{x},{y},{bytesPerRow},{height},
        var headerStr = System.Text.Encoding.ASCII.GetString(buffer[..commaIndex]);
        var parts = headerStr[2..].Split(','); // Skip "GW"

        if (parts.Length < 4)
            return MatchResult.NeedMore();

        if (!int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ||
            !int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var y) ||
            !int.TryParse(parts[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var bytesPerRow) ||
            !int.TryParse(parts[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var height))
        {
            return MatchResult.NeedMore();
        }

        var width = bytesPerRow * 8; // Convert bytes to dots (monochrome)
        var totalDataBytes = bytesPerRow * height;
        var headerEnd = commaIndex + 1; // Include comma
        var totalLength = headerEnd + totalDataBytes + 1; // +1 for newline

        // Check if we have all the data
        if (buffer.Length < totalLength)
        {
            return MatchResult.NeedMore();
        }

        // Verify newline at end (accept CR or LF)
        var terminatorByte = buffer[headerEnd + totalDataBytes];
        if (terminatorByte != 0x0A && terminatorByte != 0x0D) // LF or CR
            return MatchResult.Matched(new ParseError(null, "Invalid terminator"));

        // Extract graphics data
        var graphicsData = buffer[headerEnd..(headerEnd + totalDataBytes)].ToArray();

        var element = new PrintGraphic(x, y, width, height, graphicsData)
        {
            RawBytes = buffer[..totalLength].ToArray(),
            LengthInBytes = totalLength
        };

        return MatchResult.Matched(element);
    }
}
