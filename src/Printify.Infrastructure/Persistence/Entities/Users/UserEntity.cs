using System.ComponentModel.DataAnnotations.Schema;
using Printify.Infrastructure.Persistence.Entities;

namespace Printify.Infrastructure.Persistence.Entities.Users;

/// <summary>
/// Entity Framework model for the users table.
/// </summary>
[Table("users")]
public sealed class UserEntity : BaseEntity
{
    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Column("created_from_ip")]
    public string CreatedFromIp { get; set; } = string.Empty;
}

