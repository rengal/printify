namespace Printify.Web.Contracts.Workspaces.Responses;

public sealed record DocumentRetentionCleanupResultDto(
    int DeletedDocuments,
    int DeletedMedia);
