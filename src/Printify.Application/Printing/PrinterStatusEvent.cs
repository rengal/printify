using Printify.Domain.Printers;

namespace Printify.Application.Printing;

/// <summary>
/// Status change notification for a printer within a workspace.
/// </summary>
/// <param name="WorkspaceId">Owning workspace of the printer.</param>
/// <param name="PrinterId">Affected printer identifier.</param>
/// <param name="DesiredStatus">Desired state set by operator.</param>
/// <param name="RuntimeStatus">Observed runtime status of listener.</param>
/// <param name="UpdatedAt">Timestamp of this snapshot.</param>
/// <param name="Error">Optional error message if status is Error.</param>
public sealed record PrinterStatusEvent(
    Guid WorkspaceId,
    Guid PrinterId,
    PrinterDesiredStatus DesiredStatus,
    PrinterRuntimeStatus RuntimeStatus,
    DateTimeOffset UpdatedAt,
    string? Error);
