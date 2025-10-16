using System.ComponentModel.DataAnnotations.Schema;
using Printify.Infrastructure.Persistence.Entities;
using Printify.Infrastructure.Persistence.Entities.Users;

namespace Printify.Infrastructure.Persistence.Entities.AnonymousSessions;

/// <summary>
/// Entity Framework model for the anonymous_sessions table.
/// </summary>
[Table("anonymous_sessions")]
public sealed class AnonymousSessionEntity : BaseEntity
{
    [Column("last_active_at")]
    public DateTimeOffset LastActiveAt { get; set; }

    [Column("created_from_ip")]
    public string CreatedFromIp { get; set; } = string.Empty;

    [Column("linked_user_id")]
    public Guid? LinkedUserId { get; set; }

    [ForeignKey(nameof(LinkedUserId))]
    public UserEntity? LinkedUser { get; set; }
}
