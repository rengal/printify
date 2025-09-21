namespace Printify.Contracts.Elements;

/// <summary>
/// Sets the module size (dot width) for QR codes via GS ( k.
/// </summary>
/// <param name=\"Sequence\">Monotonic sequence index within the document stream.</param>
/// <param name=\"ModuleSize\">Width of a single QR module in dots.</param>
public sealed record SetQrModuleSize(int Sequence, int ModuleSize) : NonPrintingElement(Sequence);
