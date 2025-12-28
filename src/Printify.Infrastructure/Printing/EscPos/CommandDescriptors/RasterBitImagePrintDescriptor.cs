using Printify.Application.Interfaces;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Media;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// Command: GS v 0 m xL xH yL yH [data] - raster bit image print.
/// ASCII: GS v 0.
/// HEX: 1D 76 30 m xL xH yL yH ...
public sealed class RasterBitImagePrintDescriptor(IMediaService mediaService) : ICommandDescriptor
{
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1D, 0x76, 0x30 };
    
    // Need at least 8 bytes: GS v 0 m xL xH yL yH
    public int MinLength => 8;
    
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer)
    {
        // Extract width in bytes (little-endian)
        var widthBytes = buffer[4] | (buffer[5] << 8);
        
        // Extract height in dots (little-endian)
        var height = buffer[6] | (buffer[7] << 8);
        
        // Calculate total payload length
        var payloadLength = widthBytes * height;
        
        // Total length: 8 byte header + payload
        return 8 + payloadLength;
    }
    
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        // Extract parameters
        var mode = buffer[3];
        var widthBytes = buffer[4] | (buffer[5] << 8);
        var heightInDots = buffer[6] | (buffer[7] << 8);
        var payloadLength = widthBytes * heightInDots;
        
        // Check if we have the complete payload
        if (buffer.Length < 8 + payloadLength)
            return MatchResult.NeedMore();
        
        // Extract payload data
        var payload = buffer.Slice(8, payloadLength).ToArray();
        
        // Convert raster data to bitmap
        var widthInDots = widthBytes * 8;
        var bitmap = new MonochromeBitmap(widthInDots, heightInDots, payload);
        
        // Convert to MediaUpload using IMediaService
        var media = mediaService.ConvertToMediaUpload(bitmap, "image/png");
        
        // Create RasterImageContent element
        var element = new RasterImageUpload(widthInDots, heightInDots, media);
        
        // Total bytes consumed: header (8) + payload
        var bytesConsumed = 8 + payloadLength;
        
        // Return matched result with the raster image element
        return MatchResult.Matched(element);
    }
}

// Alternative names avoiding GS, ESC:
// - RasterBitImagePrintDescriptor
// - RasterImageCommandDescriptor
// - BitImageRasterDescriptor
// - RasterBitmapPrintDescriptor
// - RasterImageUploadDescriptor
// - RasterBitmapCommandDescriptor
// - RasterImageDataDescriptor
// - RasterImagePayloadDescriptor
// - RasterBitmapUploadDescriptor
// - RasterImageTransferDescriptor
