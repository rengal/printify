namespace Printify.Contracts.Documents.Elements;

/// <summary>
/// Renders a one-dimensional barcode using the GS k command family.
/// </summary>
/// <param name=\"Sequence\">Monotonic sequence index within the document stream.</param>
/// <param name=\"Symbology\">Selected barcode symbology.</param>
/// <param name=\"Data\">Raw data payload to encode.</param>
public sealed record PrintBarcode(int Sequence, BarcodeSymbology Symbology, string Data)
    : PrintingElement(Sequence);
