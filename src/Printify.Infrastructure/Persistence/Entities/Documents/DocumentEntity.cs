namespace Printify.Infrastructure.Persistence.Entities.Documents;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("documents")]
public sealed class DocumentEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("print_job_id")]
    public Guid PrintJobId { get; set; }

    [Column("printer_id")]
    public Guid PrinterId { get; set; }

    [Column("version")]
    public int Version { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("protocol")]
    public string Protocol { get; set; } = string.Empty;

    [Column("client_address")]
    public string? ClientAddress { get; set; }

    [InverseProperty(nameof(DocumentElementEntity.Document))]
    public List<DocumentElementEntity> Elements { get; set; } = new();

}

[Table("document_elements")]
public sealed class DocumentElementEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("document_id")]
    public Guid DocumentId { get; set; }

    [Column("sequence")]
    public int Sequence { get; set; }

    [Column("type")]
    public string ElementType { get; set; } = string.Empty;

    [Column("payload", TypeName = "TEXT")]
    public string Payload { get; set; } = string.Empty;

    [Column("media_id")]
    public Guid? MediaId { get; set; }

    [ForeignKey(nameof(DocumentId))]
    public DocumentEntity? Document { get; set; }

    [ForeignKey(nameof(MediaId))]
    public DocumentMediaEntity? Media { get; set; }
}

[Table("document_media")]
public sealed class DocumentMediaEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("owner_workspace_id")]
    public Guid? OwnerWorkspaceId { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("content_type")]
    public string ContentType { get; set; } = string.Empty;

    [Column("length")]
    public long Length { get; set; }

    [Column("checksum")]
    public string Checksum { get; set; } = string.Empty;

    [Column("file_name")]
    public string FileName { get; set; } = string.Empty;

    [Column("url")]
    public string Url { get; set; } = string.Empty;
}
