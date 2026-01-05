using System.ComponentModel.DataAnnotations.Schema;

namespace Printify.Infrastructure.Persistence.Entities.Printers;

[Table("printers")]
public sealed class PrinterEntity : BaseEntity
{
    [Column("owner_workspace_id")]
    public Guid OwnerWorkspaceId { get; set; }

    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Column("created_from_ip")]
    public string CreatedFromIp { get; set; } = string.Empty;

    public PrinterSettingsEntity Settings { get; set; } = new();

    [Column("is_pinned")]
    public bool IsPinned { get; set; }

    [Column("last_viewed_document_id")]
    public Guid? LastViewedDocumentId { get; set; }

    [Column("last_document_received_at")]
    public DateTimeOffset? LastDocumentReceivedAt { get; set; }
}
