namespace Printify.Domain.Documents.Elements;

/// <summary>
/// Selects the placement of human-readable barcode labels via GS H (0x1D 0x48).
/// </summary>
/// <param name=\"Sequence\">Monotonic sequence index within the document stream.</param>
/// <param name=\"Position\">Desired label positioning.</param>
public sealed record SetBarcodeLabelPosition(int Sequence, BarcodeLabelPosition Position) : NonPrintingElement(Sequence);
