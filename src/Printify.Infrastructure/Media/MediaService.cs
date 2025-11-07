using System.Security.Cryptography;
using Printify.Application.Interfaces;
using Printify.Domain.Media;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Printify.Infrastructure.Media;

/// <summary>
/// Converts monochrome bitmaps to media upload format using ImageSharp.
/// </summary>
public sealed class MediaService : IMediaService
{
    /// <inheritdoc />
    public MediaUpload ConvertToMediaUpload(MonochromeBitmap bitmap, string format = "image/png")
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentNullException.ThrowIfNull(format);

        // Convert packed bits to ImageSharp image
        using var image = new Image<L8>(bitmap.Width, bitmap.Height);

        for (int y = 0; y < bitmap.Height; y++)
        {
            var rowOffset = y * bitmap.Stride;
            Span<L8> row = image.DangerousGetPixelRowMemory(y).Span;
            
            for (int x = 0; x < bitmap.Width; x++)
            {
                var byteIndex = rowOffset + (x / 8);
                var bitIndex = 7 - (x % 8); // MSB = leftmost pixel
                var isSet = (bitmap.Data[byteIndex] & (1 << bitIndex)) != 0;

                // White (255) for set bits, black (0) for unset bits
                row[x] = new L8(isSet ? (byte)255 : (byte)0);
            }
        }

        // Encode to specified format
        using var ms = new MemoryStream();
        IImageEncoder encoder = format.ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" => new JpegEncoder { Quality = 90 },
            "image/png" or _ => new PngEncoder()
        };

        image.Save(ms, encoder);
        var content = ms.ToArray();

        // Calculate checksum
        var checksum = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

        return new MediaUpload(
            ContentType: format,
            Length: content.Length,
            Checksum: checksum,
            Content: content
        );
    }
}