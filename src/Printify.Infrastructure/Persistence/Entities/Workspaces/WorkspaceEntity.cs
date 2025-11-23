using System.ComponentModel.DataAnnotations.Schema;

namespace Printify.Infrastructure.Persistence.Entities.Workspaces;

/// <summary>
/// Entity Framework model for the workspaces table.
/// </summary>
[Table("workspaces")]
public sealed class WorkspaceEntity : BaseEntity
{
    [Column("owner_name")]
    public string OwnerName { get; set; } = string.Empty;

    [Column("token")]
    public string Token { get; set; } = string.Empty;

    [Column("created_from_ip")]
    public string CreatedFromIp { get; set; } = string.Empty;
}

