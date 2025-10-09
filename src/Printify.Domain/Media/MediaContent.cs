namespace Printify.Domain.Media;

public sealed record MediaContent(
    string ContentType,
    long? Length,
    string? Checksum,
    ReadOnlyMemory<byte>? Content
);
