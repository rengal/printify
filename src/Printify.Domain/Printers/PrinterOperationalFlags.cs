namespace Printify.Domain.Printers;

using Mediator.Net.Contracts;

/// <summary>
/// Persisted operational flags and desired lifecycle for a printer.
/// Add fields here when they represent device conditions or operator intent that should survive restarts.
/// </summary>
public sealed record PrinterOperationalFlags(
    Guid PrinterId,
    PrinterTargetState TargetState,
    DateTimeOffset UpdatedAt,
    bool IsCoverOpen,
    bool IsPaperOut,
    bool IsOffline,
    bool HasError,
    bool IsPaperNearEnd) : IResponse;

