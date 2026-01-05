namespace Printify.Web.Contracts.Printers.Responses;

/// <summary>
/// Partial update for operational flags emitted in active printer streams.
/// Null fields indicate no change and must not overwrite stored values.
/// </summary>
/// <param name="PrinterId">Identifier of the printer the update belongs to.</param>
/// <param name="UpdatedAt">Timestamp when the update was captured.</param>
/// <param name="TargetState">Desired lifecycle state (Started/Stopped).</param>
/// <param name="IsCoverOpen">Indicates the printer cover is open.</param>
/// <param name="IsPaperOut">Indicates the printer is out of paper.</param>
/// <param name="IsOffline">Indicates the printer is offline.</param>
/// <param name="HasError">Indicates the printer has an error condition.</param>
/// <param name="IsPaperNearEnd">Indicates paper is near end.</param>
public sealed record PrinterOperationalFlagsUpdateDto(
    Guid PrinterId,
    DateTimeOffset UpdatedAt,
    string? TargetState,
    bool? IsCoverOpen,
    bool? IsPaperOut,
    bool? IsOffline,
    bool? HasError,
    bool? IsPaperNearEnd);
