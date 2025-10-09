namespace Printify.Domain.Documents.Elements;

/// <summary>
/// Sets line spacing in printer dots for subsequent lines (e.g., ESC 0x1B 0x33 n).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Spacing">Line spacing value in dots.</param>
public sealed record SetLineSpacing(int Sequence, int Spacing) : NonPrintingElement(Sequence);

