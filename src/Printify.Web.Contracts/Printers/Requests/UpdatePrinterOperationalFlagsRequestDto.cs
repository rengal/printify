namespace Printify.Web.Contracts.Printers.Requests;

/// <summary>
/// Patchable operational flags for a printer.
/// </summary>
public sealed record UpdatePrinterOperationalFlagsRequestDto(
    bool? IsCoverOpen,
    bool? IsPaperOut,
    bool? IsOffline,
    bool? HasError,
    bool? IsPaperNearEnd,
    string? TargetState = null);
