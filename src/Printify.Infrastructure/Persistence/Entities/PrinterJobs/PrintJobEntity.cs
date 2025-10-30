using System.ComponentModel.DataAnnotations.Schema;

namespace Printify.Infrastructure.Persistence.Entities.PrinterJobs;

[Table("print_jobs")]
public sealed class PrintJobEntity : BaseEntity
{
    [Column("printer_id")]
    public Guid PrinterId { get; set; }

    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Column("protocol")]
    public string Protocol { get; set; } = string.Empty;

    [Column("width_in_dots")]
    public int WidthInDots { get; set; }

    [Column("height_in_dots")]
    public int? HeightInDots { get; set; }

    [Column("client_address")]
    public string ClientAddress { get; set; } = string.Empty;
}
