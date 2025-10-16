using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Printify.Infrastructure.Persistence.Entities.Users;

/// <summary>
/// Entity Framework model for the users table.
/// </summary>
[Table("users")]
public sealed class UserEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("created_from_ip")]
    public string CreatedFromIp { get; set; } = string.Empty;
}
