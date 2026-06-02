namespace Printify.Infrastructure.Retention;

public sealed record DocumentRetentionCleanupResult(
    int DeletedDocuments,
    int DeletedMedia);

public sealed record DocumentRetentionCleanupSummary(
    int ExpiredDocuments,
    int RetentionMediaFiles);
