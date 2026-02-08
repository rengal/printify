using Printify.Infrastructure.Printing.Epl.Commands;
using Printify.Infrastructure.Printing.Common;
using System.Globalization;
using Printify.Domain.Printing;
using Printify.Application.Interfaces;
using Printify.Domain.Media;

/// <summary>
/// Command: GW x, y, bytesPerRow, height, - Graphic write.
/// ASCII: GW {x},{y},{bytesPerRow},{height},
/// HEX: 47 57 {x},{y},{bytesPerRow},{height},
/// Followed by binary graphics data (bytesPerRow * height bytes) and then newline.
/// </summary>
/// <remarks>
/// This descriptor is special - it has a useful TryGetExactLength implementation
/// because the command length can be calculated from the header parameters.
/// Creates an EplRasterImageUpload command with MediaUpload for finalization.
/// </summary>
public sealed class PrintGraphicDescriptor(IMediaService mediaService) : ICommandDescriptor
{
    private const int MinLen = 10; // 'GW' + minimum params

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x47, 0x57 }; // 'GW'
    public int MinLength => MinLen;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer)
    {
        // GW command format: GW{x},{y},{bytesPerRow},{height},
        // We need to find the comma AFTER all 4 parameters (the 4th comma overall)
        // First 2 chars are "GW", then we have 4 comma-separated values, then a final comma
        var commaCount = 0;
        var headerEndIndex = -1;

        for (var i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] == ',')
            {
                commaCount++;
                if (commaCount == 4) // 4th comma is the one after height
                {
                    headerEndIndex = i;
                    break;
                }
            }
        }

        if (headerEndIndex < 0)
            return null; // Haven't received all header parameters yet

        // Parse the header to get total data bytes
        var headerStr = System.Text.Encoding.ASCII.GetString(buffer[..headerEndIndex]);
        var parts = headerStr[2..].Split(','); // Skip "GW"

        if (parts.Length < 4)
            return null;

        if (!int.TryParse(parts[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var bytesPerRow) ||
            !int.TryParse(parts[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var height))
            return null;

        var totalDataBytes = bytesPerRow * height;
        var totalLength = headerEndIndex + 1 + totalDataBytes + 1; // +1 for comma, +1 for newline

        // Only return exact length if we have enough data
        return buffer.Length >= totalLength ? totalLength : null;
    }

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        // GW command format: GW{x},{y},{bytesPerRow},{height},
        // Find the comma AFTER all 4 parameters (the 4th comma overall)
        var commaCount = 0;
        var headerEndIndex = -1;

        for (var i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] == ',')
            {
                commaCount++;
                if (commaCount == 4) // 4th comma is the one after height
                {
                    headerEndIndex = i;
                    break;
                }
            }
        }

        if (headerEndIndex < 0)
            return MatchResult.NeedMore();

        // Parse header: GW{x},{y},{bytesPerRow},{height},
        var headerStr = System.Text.Encoding.ASCII.GetString(buffer[..headerEndIndex]);
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
        var headerEnd = headerEndIndex + 1; // Include comma
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

        // Convert raster data to bitmap (same as EscPos GS v 0)
        var bitmap = new MonochromeBitmap(width, height, graphicsData);

        // Convert to MediaUpload using IMediaService
        var media = mediaService.ConvertToMediaUpload(bitmap, "image/png");

        // Create EplRasterImageUpload element instead of PrintGraphic
        var element = new EplRasterImageUpload(x, y, width, height, media)
        {
            RawBytes = buffer[..totalLength].ToArray(),
            LengthInBytes = totalLength
        };

        return MatchResult.Matched(element);
    }
}
