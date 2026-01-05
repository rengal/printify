using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Printify.Infrastructure.Persistence.Entities.Printers;

[Owned]
public sealed class PrinterSettingsEntity
{
    [Column("protocol")]
    public string Protocol { get; set; } = string.Empty;

    [Column("width_in_dots")]
    public int WidthInDots { get; set; }

    [Column("height_in_dots")]
    public int? HeightInDots { get; set; }

    [Column("listen_tcp_port_number")]
    public int ListenTcpPortNumber { get; set; }

    [Column("emulate_buffer_capacity")]
    public bool EmulateBufferCapacity { get; set; }

    [Column("buffer_drain_rate")]
    public decimal? BufferDrainRate { get; set; }

    [Column("buffer_max_capacity")]
    public int? BufferMaxCapacity { get; set; }
}
