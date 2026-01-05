namespace Printify.Web.Contracts.Printers.Requests;

/// <summary>
/// Patchable realtime emulation status fields for a printer.
/// </summary>
public sealed record UpdatePrinterRealtimeStatusRequestDto(
    string? TargetStatus,
    bool? IsCoverOpen,
    bool? IsPaperOut,
    bool? IsOffline,
    bool? HasError,
    bool? IsPaperNearEnd,
    string? Drawer1State,
    string? Drawer2State);
