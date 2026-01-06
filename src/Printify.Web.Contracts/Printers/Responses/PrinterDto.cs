using System.Text.Json.Serialization;

namespace Printify.Web.Contracts.Printers.Responses;

/// <summary>
/// Printer identity and metadata exposed to clients.
/// </summary>
/// <param name="Id">Database-generated identifier.</param>
/// <param name="DisplayName">Friendly name shown in UI.</param>
/// <param name="IsPinned">Indicates whether the printer is pinned for quick access.</param>
/// <param name="LastViewedDocumentId">Identifier of the last viewed document.</param>
/// <param name="LastDocumentReceivedAt">Timestamp of the most recently persisted document for this printer.</param>
public sealed record PrinterDto(
    Guid Id,
    string DisplayName,
    bool IsPinned,
    Guid? LastViewedDocumentId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    DateTimeOffset? LastDocumentReceivedAt);
