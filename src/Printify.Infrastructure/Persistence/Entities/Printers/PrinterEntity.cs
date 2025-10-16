using System.ComponentModel.DataAnnotations.Schema;
using Printify.Infrastructure.Persistence.Entities;

namespace Printify.Infrastructure.Persistence.Entities.Printers;

[Table("printers")]
public sealed class PrinterEntity : BaseEntity
{
    [Column("owner_user_id")]
    public Guid? OwnerUserId { get; set; }

    [Column("owner_anonymous_session_id")]
    public Guid? OwnerAnonymousSessionId { get; set; }

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
}
