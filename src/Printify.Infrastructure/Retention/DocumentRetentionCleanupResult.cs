namespace Printify.Infrastructure.Retention;

public sealed record DocumentRetentionCleanupResult(
    int DeletedDocuments,
    int DeletedMedia);
