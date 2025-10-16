namespace Printify.Domain;

/// <summary>
/// Provides common audit information for domain entities.
/// </summary>
/// <typeparam name="TId">Identifier type used by the entity.</typeparam>
public abstract record BaseDomainEntity<TId>(TId Id, DateTimeOffset CreatedAt, bool IsDeleted);
