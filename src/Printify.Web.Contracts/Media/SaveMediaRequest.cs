namespace Printify.Web.Contracts.Media;

public sealed record SaveMediaRequest(
    string ContentType,
    long? SizeBytes,
    string? Sha256,
    ReadOnlyMemory<byte>? Content
);