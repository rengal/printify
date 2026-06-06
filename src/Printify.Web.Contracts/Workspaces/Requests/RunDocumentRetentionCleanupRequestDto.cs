namespace Printify.Web.Contracts.Workspaces.Requests;

public sealed record RunDocumentRetentionCleanupRequestDto(
    int MaxDocuments,
    // Optional admin override (in days) applied to every workspace; 0 deletes all documents, null uses per-workspace settings.
    int? RetentionDaysOverride);
