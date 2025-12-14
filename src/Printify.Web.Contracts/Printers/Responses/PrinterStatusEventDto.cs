namespace Printify.Web.Contracts.Printers.Responses;

/// <summary>
/// Status event sent over SSE for printer runtime updates.
/// </summary>
/// <param name="PrinterId">Identifier of the printer.</param>
/// <param name="TargetState">Target state requested by the operator.</param>
/// <param name="RuntimeStatus">Observed runtime status of the listener.</param>
/// <param name="UpdatedAt">Timestamp when this snapshot was captured.</param>
/// <param name="Error">Optional error message if runtime status is error.</param>
public sealed record PrinterStatusEventDto(
    Guid PrinterId,
    string TargetState,
    string RuntimeStatus,
    DateTimeOffset UpdatedAt,
    string? Error);
