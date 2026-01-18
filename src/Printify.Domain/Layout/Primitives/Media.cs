namespace Printify.Domain.Layout.Primitives;

/// <summary>
/// Media reference for image elements.
/// </summary>
public sealed record Media(
    string MimeType,
    int Size,
    string Url,
    string StorageKey);
