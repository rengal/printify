using System.ComponentModel.DataAnnotations.Schema;

namespace Printify.Infrastructure.Persistence.Entities.Printers;

[Table("printers")]
public sealed class PrinterEntity : BaseEntity
{
    [Column("owner_workspace_id")]
    public Guid OwnerWorkspaceId { get; set; }

    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Column("protocol")]
    public string Protocol { get; set; } = string.Empty;

    [Column("width_in_dots")]
    public int WidthInDots { get; set; }

    [Column("height_in_dots")]
    public int? HeightInDots { get; set; }

    [Column("created_from_ip")]
    public string CreatedFromIp { get; set; } = string.Empty;

    [Column("listen_tcp_port_number")]
    public int ListenTcpPortNumber { get; set; }

    [Column("emulate_buffer_capacity")]
    public bool EmulateBufferCapacity { get; set; }

    [Column("buffer_drain_rate")]
    public decimal? BufferDrainRate { get; set; }

    [Column("buffer_max_capacity")]
    public int? BufferMaxCapacity { get; set; }

    [Column("target_status")]
    public string TargetStatus { get; set; } = "Started";

    [Column("runtime_status")]
    public string RuntimeStatus { get; set; } = "Unknown";

    [Column("runtime_status_updated_at")]
    public DateTimeOffset? RuntimeStatusUpdatedAt { get; set; }

    [Column("runtime_status_error")]
    public string? RuntimeStatusError { get; set; }

    [Column("is_pinned")]
    public bool IsPinned { get; set; }

    [Column("last_viewed_document_id")]
    public Guid? LastViewedDocumentId { get; set; }

    [Column("last_document_received_at")]
    public DateTimeOffset? LastDocumentReceivedAt { get; set; }
}
