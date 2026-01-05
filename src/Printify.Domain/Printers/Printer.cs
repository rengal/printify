namespace Printify.Domain.Printers;

/// <summary>
/// Represents virtual printer.
/// </summary>
/// <param name="Id">Database-generated identifier.</param>
/// <param name="OwnerWorkspaceId">Identifier of the workspace that owns the printer, if claimed.</param>
/// <param name="DisplayName">Friendly name shown in UI.</param>
/// <param name="CreatedAt">Registration timestamp in UTC.</param>
/// <param name="CreatedFromIp">IP address captured when the printer was registered.</param>
/// <param name="RuntimeStatusUpdatedAt">
/// Timestamp when <paramref name="RuntimeStatus"/> was last updated.
/// </param>
/// <param name="IsPinned">Indicates whether the printer is pinned for quick access.</param>
/// <param name="IsDeleted">Soft-delete marker for the printer.</param>
/// <param name="LastViewedDocumentId">Identifier of the last document viewed for this printer.</param>
/// <param name="LastDocumentReceivedAt">Timestamp of the most recently persisted document for this printer.</param>
public sealed record Printer(
    Guid Id,
    Guid OwnerWorkspaceId,
    string DisplayName,
    DateTimeOffset CreatedAt,
    string CreatedFromIp,
    DateTimeOffset? RuntimeStatusUpdatedAt,
    bool IsPinned,
    bool IsDeleted,
    Guid? LastViewedDocumentId,
    DateTimeOffset? LastDocumentReceivedAt)
    : BaseDomainEntity(Id, CreatedAt, IsDeleted);
