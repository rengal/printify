namespace Printify.Domain.Printers;

/// <summary>
/// Unified update payload for printer status streaming.
/// Includes runtime updates and optional printer/settings changes.
/// </summary>
public sealed record PrinterStatusUpdate(
    Guid PrinterId,
    DateTimeOffset UpdatedAt,
    PrinterRuntimeStatusUpdate? RuntimeUpdate = null,
    PrinterOperationalFlagsUpdate? OperationalFlagsUpdate = null,
    PrinterSettings? Settings = null,
    Printer? Printer = null);
