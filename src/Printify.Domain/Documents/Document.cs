using Printify.Domain.Documents.Elements;
using Printify.Domain.Printers;

namespace Printify.Domain.Documents;

/// <summary>
/// Represents a snapshot of a parsed document emitted when a print job completes.
/// </summary>
public sealed record Document(
    Guid Id,
    Guid PrintJobId,
    Guid PrinterId,
    int Version,
    DateTimeOffset CreatedAt,
    Protocol Protocol,
    int WidthInDots,
    int? HeightInDots,
    string? ClientAddress,
    int BytesReceived,
    int BytesSent,
    IReadOnlyCollection<Element> Elements)
{
    public const int CurrentVersion = 1;
}
