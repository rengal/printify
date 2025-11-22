namespace Printify.Domain;

/// <summary>
/// Provides common audit information for domain entities.
/// </summary>
public abstract record BaseDomainEntity(Guid Id, DateTimeOffset CreatedAt, bool IsDeleted);
