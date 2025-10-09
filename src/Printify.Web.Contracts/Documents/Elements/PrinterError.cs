namespace Printify.Web.Contracts.Documents.Elements;

/// <summary>
/// Represents a printer-specific error emitted during tokenization (e.g., simulated buffer overflow).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Message">Human-readable description of the error.</param>
public sealed record PrinterError(int Sequence, string Message) : NonPrintingElement(Sequence);
