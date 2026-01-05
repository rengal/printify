using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Printify.Infrastructure.Persistence.Entities.Printers;

[Table("printer_realtime_status")]
public sealed class PrinterRealtimeStatusEntity
{
    [Key]
    [Column("printer_id")]
    public Guid PrinterId { get; set; }

    [Column("target_state")]
    public string TargetState { get; set; } = "Stopped";

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [Column("buffered_bytes")]
    public int BufferedBytes { get; set; }

    [Column("cover_open")]
    public bool IsCoverOpen { get; set; }

    [Column("paper_out")]
    public bool IsPaperOut { get; set; }

    [Column("is_offline")]
    public bool IsOffline { get; set; }

    [Column("has_error")]
    public bool HasError { get; set; }

    [Column("paper_near_end")]
    public bool IsPaperNearEnd { get; set; }

    [Column("drawer_1_state")]
    public string Drawer1State { get; set; } = "Closed";

    [Column("drawer_2_state")]
    public string Drawer2State { get; set; } = "Closed";
}
