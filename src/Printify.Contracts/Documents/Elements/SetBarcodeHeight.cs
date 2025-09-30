namespace Printify.Contracts.Documents.Elements;

/// <summary>
/// Configures the height of subsequent barcodes using GS h (0x1D 0x68).
/// </summary>
/// <param name=\"Sequence\">Monotonic sequence index within the document stream.</param>
/// <param name=\"HeightInDots\">Barcode height in dots.</param>
public sealed record SetBarcodeHeight(int Sequence, int HeightInDots) : NonPrintingElement(Sequence);
