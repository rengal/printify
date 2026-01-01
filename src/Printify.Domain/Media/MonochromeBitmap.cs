namespace Printify.Domain.Media;

/// <summary>
/// Represents a memory-efficient monochrome (1-bit per pixel) bitmap.
/// Uses packed bits for minimal memory footprint - 1 byte stores 8 pixels.
///
/// Bit Semantics:
/// - Set bit (1): Dot is marked/printed (black dot on thermal paper)
/// - Unset bit (0): Dot is not marked (transparent, no printing occurs)
/// </summary>
public sealed class MonochromeBitmap
{
    /// <summary>
    /// Gets the width of the bitmap in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height of the bitmap in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the raw packed bit data.
    /// Each byte contains 8 pixels (MSB = leftmost pixel).
    /// Row data is byte-aligned (padded if Width is not divisible by 8).
    /// Set bits (1) represent marked dots, unset bits (0) represent unmarked (transparent) dots.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// Gets the stride (bytes per row) accounting for byte alignment.
    /// </summary>
    public int Stride { get; }

    /// <summary>
    /// Initializes a new monochrome bitmap with the specified dimensions.
    /// </summary>
    /// <param name="width">Width in pixels (must be positive).</param>
    /// <param name="height">Height in pixels (must be positive).</param>
    /// <exception cref="ArgumentOutOfRangeException">Width or height is less than 1.</exception>
    public MonochromeBitmap(int width, int height)
    {
        if (width < 1)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be at least 1.");
        if (height < 1)
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be at least 1.");

        Width = width;
        Height = height;
        // Calculate stride: number of bytes needed per row (rounded up to nearest byte)
        Stride = (width + 7) / 8;
        Data = new byte[Stride * height];
    }

    /// <summary>
    /// Initializes a monochrome bitmap from existing packed data.
    /// </summary>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="data">Pre-packed bit data (must match calculated stride * height).</param>
    /// <exception cref="ArgumentException">Data length doesn't match expected size.</exception>
    public MonochromeBitmap(int width, int height, byte[] data)
    {
        if (width < 1)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be at least 1.");
        if (height < 1)
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be at least 1.");

        Width = width;
        Height = height;
        Stride = (width + 7) / 8;
        
        var expectedSize = Stride * height;
        if (data.Length != expectedSize)
            throw new ArgumentException($"Data length {data.Length} doesn't match expected {expectedSize} bytes.", nameof(data));

        Data = data;
    }
}