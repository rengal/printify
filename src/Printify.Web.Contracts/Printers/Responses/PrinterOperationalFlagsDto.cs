namespace Printify.Web.Contracts.Printers.Responses;

/// <summary>
/// Persisted operational flags and desired lifecycle state.
/// </summary>
/// <param name="TargetState">Desired lifecycle state (Started/Stopped).</param>
/// <param name="UpdatedAt">Timestamp when the flags were captured.</param>
/// <param name="IsCoverOpen">Indicates the printer cover is open.</param>
/// <param name="IsPaperOut">Indicates the printer is out of paper.</param>
/// <param name="IsOffline">Indicates the printer is offline.</param>
/// <param name="HasError">Indicates the printer has an error condition.</param>
/// <param name="IsPaperNearEnd">Indicates paper is near end.</param>
public sealed record PrinterOperationalFlagsDto(
    Guid PrinterId,
    string TargetState,
    DateTimeOffset UpdatedAt,
    bool IsCoverOpen,
    bool IsPaperOut,
    bool IsOffline,
    bool HasError,
    bool IsPaperNearEnd);
