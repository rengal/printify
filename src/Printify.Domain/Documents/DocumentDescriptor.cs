using Printify.Domain.Printers;

namespace Printify.Domain.Documents;

/// <summary>
/// Summary view of a document used for list endpoints and dashboards.
/// </summary>
/// <param name="Id">Document identifier.</param>
/// <param name="PrinterId">Printer identifier associated with the document.</param>
/// <param name="Timestamp">Timestamp when the document was captured.</param>
/// <param name="Protocol">Protocol used to parse the document.</param>
/// <param name="SourceIp">Optional source IP associated with the document.</param>
/// <param name="ElementCount">Number of elements contained in the document.</param>
/// <param name="HasRasterImages">Indicates whether the document includes raster images.</param>
/// <param name="PreviewText">First non-empty text line to use as a preview snippet.</param>
public sealed record DocumentDescriptor(
    long Id,
    long PrinterId,
    DateTimeOffset Timestamp,
    Protocol Protocol,
    string? SourceIp,
    int ElementCount,
    bool HasRasterImages,
    string? PreviewText);
