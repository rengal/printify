namespace Printify.Web.Contracts.Printers.Responses;

/// <summary>
/// Partial realtime status update emitted via SSE; null fields indicate no change.
/// </summary>
/// <param name="PrinterId">Identifier of the printer the update belongs to.</param>
/// <param name="UpdatedAt">Timestamp when the update was captured.</param>
/// <param name="TargetState">Desired lifecycle state (Started/Stopped).</param>
/// <param name="State">Observed listener state.</param>
/// <param name="BufferedBytes">Current emulated input buffer usage in bytes.</param>
/// <param name="IsCoverOpen">Indicates the printer cover is open.</param>
/// <param name="IsPaperOut">Indicates the printer is out of paper.</param>
/// <param name="IsOffline">Indicates the printer is offline.</param>
/// <param name="HasError">Indicates the printer has an error condition.</param>
/// <param name="IsPaperNearEnd">Indicates paper is near end.</param>
/// <param name="Drawer1State">Emulated drawer 1 state.</param>
/// <param name="Drawer2State">Emulated drawer 2 state.</param>
public sealed record PrinterRealtimeStatusUpdateDto(
    Guid PrinterId,
    DateTimeOffset UpdatedAt,
    string? TargetState,
    string? State,
    int? BufferedBytes,
    bool? IsCoverOpen,
    bool? IsPaperOut,
    bool? IsOffline,
    bool? HasError,
    bool? IsPaperNearEnd,
    string? Drawer1State,
    string? Drawer2State);
