using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Printify.Infrastructure.Persistence.Entities;

/// <summary>
/// Base persistence entity with audit columns.
/// </summary>
public abstract class BaseEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}
