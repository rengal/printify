using System;

namespace Printify.Contracts.Media
{
    /// <summary>
    /// Bytes + metadata for a media payload (e.g., printer raster image).
    /// Use in domain/detailed-read flows. For fast reads, <see cref="Content"/> may be null.
    /// </summary>
    /// <param name="Meta">Shared media metadata.</param>
    /// <param name="Content">Raw bytes; null when excluded from read responses.</param>
    public sealed record MediaContent(
        MediaMeta Meta,
        ReadOnlyMemory<byte>? Content
    );
}
