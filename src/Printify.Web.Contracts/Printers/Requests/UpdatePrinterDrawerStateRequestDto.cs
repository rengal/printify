namespace Printify.Web.Contracts.Printers.Requests;

/// <summary>
/// Patchable drawer state for manual open/close operations.
/// </summary>
public sealed record UpdatePrinterDrawerStateRequestDto(
    string? Drawer1State,
    string? Drawer2State);
