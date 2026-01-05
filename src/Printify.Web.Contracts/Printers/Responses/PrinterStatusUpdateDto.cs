namespace Printify.Web.Contracts.Printers.Responses;

/// <summary>
/// Unified update payload for active printer streaming.
/// </summary>
/// <param name="PrinterId">Identifier of the printer the update belongs to.</param>
/// <param name="UpdatedAt">Timestamp when the update was captured.</param>
/// <param name="Runtime">Runtime-only updates when state, buffer, or drawers change.</param>
/// <param name="OperationalFlags">Partial operational flag updates when control flags change.</param>
/// <param name="Settings">Full settings payload when configuration changes.</param>
/// <param name="Printer">Printer metadata when identity changes (e.g., display name or pin state).</param>
public sealed record PrinterStatusUpdateDto(
    Guid PrinterId,
    DateTimeOffset UpdatedAt,
    PrinterRuntimeStatusUpdateDto? Runtime,
    PrinterOperationalFlagsUpdateDto? OperationalFlags,
    PrinterSettingsDto? Settings,
    PrinterDto? Printer);
