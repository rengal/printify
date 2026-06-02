namespace Printify.Web.Contracts.Workspaces.Requests;

public sealed record RunDocumentRetentionCleanupRequestDto(
    int MaxDocuments);
