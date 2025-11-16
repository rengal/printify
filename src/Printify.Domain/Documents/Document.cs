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
    string? ClientAddress,
    IReadOnlyCollection<Element> Elements)
{
    public const int CurrentVersion = 1;
}
