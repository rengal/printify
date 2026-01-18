namespace Printify.Domain.Layout.Primitives;

/// <summary>
/// Image/graphic primitive (barcode, QR code, photo, logo).
/// Decoupled from protocol commands - represents final rendered image.
/// </summary>
public sealed record ImageElement(
    Media Media,
    int X,
    int Y,
    int Width,
    int Height,
    Rotation Rotation) : BaseElement;
