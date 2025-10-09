namespace Printify.Domain.Documents.Elements;

/// <summary>
/// Resets the printer to its power-on state (ESC @).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public sealed record ResetPrinter(int Sequence) : NonPrintingElement(Sequence);
