using Printify.Domain.Media;

namespace Printify.Application.Interfaces;

/// <summary>
/// Converts raw bitmap data to MediaUpload format.
/// Implementation can use any image library (SkiaSharp, System.Drawing, ImageSharp, etc.).
/// </summary>
public interface IMediaService
{
    /// <summary>
    /// Converts a monochrome bitmap to MediaUpload with specified format.
    /// </summary>
    /// <param name="bitmap">Raw bitmap data.</param>
    /// <param name="format">Target image format (e.g., "image/png", "image/jpeg").</param>
    /// <returns>MediaUpload ready for transmission or storage.</returns>
    MediaUpload ConvertToMediaUpload(MonochromeBitmap bitmap, string format = "image/png");
}