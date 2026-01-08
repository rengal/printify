namespace Printify.Web.Contracts.Printers.Responses;

/// <summary>
/// Runtime-only status emitted via SSE and not persisted.
/// </summary>
/// <param name="PrinterId">Identifier of the printer the update belongs to.</param>
/// <param name="State">Observed listener state.</param>
/// <param name="UpdatedAt">Timestamp when the update was captured.</param>
/// <param name="BufferedBytes">Current emulated input buffer usage in bytes.</param>
/// <param name="BufferedBytesDeltaBps">Rate of change of buffered bytes in bytes per second.</param>
/// <param name="Drawer1State">Emulated drawer 1 state.</param>
/// <param name="Drawer2State">Emulated drawer 2 state.</param>
public sealed record PrinterRuntimeStatusDto(
    Guid PrinterId,
    string State,
    DateTimeOffset UpdatedAt,
    int? BufferedBytes,
    int? BufferedBytesDeltaBps,
    string? Drawer1State,
    string? Drawer2State);
