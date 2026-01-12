using Printify.Domain.Documents.Elements;
using Printify.Domain.Documents.Elements.Epl;
using Printify.Infrastructure.Printing.Common;
using System.Globalization;

namespace Printify.Infrastructure.Printing.Epl.CommandDescriptors;

/// Command: GW x, y, bytesPerRow, height, - Graphic write.
/// ASCII: GW {x},{y},{bytesPerRow},{height},
/// HEX: 47 57 {x},{y},{bytesPerRow},{height},
/// Followed by binary graphics data (bytesPerRow * height bytes) and then newline.
public sealed class EplGWGraphicWriteDescriptor : ICommandDescriptor<EplParserState>
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
        var parts = headerStr[(2)..].Split(','); // Skip "GW"

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

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, EplParserState state)
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

        // Extract graphics data
        var graphicsData = buffer[headerEnd..(headerEnd + totalDataBytes)].ToArray();

        // Verify newline at end
        if (buffer[headerEnd + totalDataBytes] != (byte)'\n')
            return MatchResult.NeedMore();

        var element = new PrintGraphic(x, y, width, height, graphicsData)
        {
            CommandRaw = Convert.ToHexString(buffer[..totalLength]),
            LengthInBytes = totalLength
        };

        return MatchResult.Matched(element);
    }
}
