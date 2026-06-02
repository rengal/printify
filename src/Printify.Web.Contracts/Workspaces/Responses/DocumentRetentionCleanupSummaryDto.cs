namespace Printify.Web.Contracts.Workspaces.Responses;

public sealed record DocumentRetentionCleanupSummaryDto(
    int ExpiredDocuments,
    int RetentionMediaFiles);
