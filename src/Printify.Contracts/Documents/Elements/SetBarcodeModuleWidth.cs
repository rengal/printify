namespace Printify.Contracts.Documents.Elements;

/// <summary>
/// Configures the module width (basic bar width) for subsequent barcodes using GS w (0x1D 0x77).
/// </summary>
/// <param name=\"Sequence\">Monotonic sequence index within the document stream.</param>
/// <param name=\"ModuleWidth\">Module width in device units (typically dots).</param>
public sealed record SetBarcodeModuleWidth(int Sequence, int ModuleWidth) : NonPrintingElement(Sequence);
