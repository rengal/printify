namespace Printify.Contracts.Documents.Elements;

/// <summary>
/// Selects justification for subsequent printable data using ESC a (0x1B 0x61).
/// </summary>
/// <param name=\"Sequence\">Monotonic sequence index within the document stream.</param>
/// <param name=\"Justification\">Requested alignment value.</param>
public sealed record SetJustification(int Sequence, TextJustification Justification) : NonPrintingElement(Sequence);
