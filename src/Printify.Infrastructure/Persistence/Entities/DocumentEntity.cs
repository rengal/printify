using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Printify.Infrastructure.Persistence.Entities;

[Table("Documents")]
public sealed class DocumentEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid PrintJobId { get; set; }

    public Guid PrinterId { get; set; }

    public int Version { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string Protocol { get; set; } = string.Empty;

    public string? ClientAddress { get; set; }

    public List<DocumentElementEntity> Elements { get; set; } = new();
}

[Table("DocumentElements")]
public sealed class DocumentElementEntity
{
    [Key]
    public Guid Id { get; set; }

    [ForeignKey(nameof(Document))]
    public Guid DocumentId { get; set; }

    public int Sequence { get; set; }

    public string Type { get; set; } = string.Empty;

    [Column(TypeName = "TEXT")]
    public string Payload { get; set; } = string.Empty;

    public DocumentEntity? Document { get; set; }
}
