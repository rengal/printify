namespace Printify.Contracts.Documents.Elements;

/// <summary>
/// A decoded printer status byte with optional human-readable description.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="StatusByte">Raw status byte value.</param>
/// <param name="Description">Optional decoded description for UI/debugging.</param>
public sealed record PrinterStatus(int Sequence, byte StatusByte, string? Description)
    : NonPrintingElement(Sequence);
