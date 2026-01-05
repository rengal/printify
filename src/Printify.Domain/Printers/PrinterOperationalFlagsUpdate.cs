namespace Printify.Domain.Printers;

/// <summary>
/// Partial update for persisted operational flags.
/// Null fields indicate no change and must not overwrite stored values.
/// </summary>
public sealed record PrinterOperationalFlagsUpdate(
    Guid PrinterId,
    DateTimeOffset UpdatedAt,
    PrinterTargetState? TargetState = null,
    bool? IsCoverOpen = null,
    bool? IsPaperOut = null,
    bool? IsOffline = null,
    bool? HasError = null,
    bool? IsPaperNearEnd = null);
