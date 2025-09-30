namespace Printify.Contracts.Documents.Elements;

/// <summary>
/// Enables or disables emphasized (bold) text mode (ESC E).
/// </summary>
/// <param name=\"Sequence\">Monotonic sequence index within the document stream.</param>
/// <param name=\"IsEnabled\">True when bold mode is turned on; false when turned off.</param>
public sealed record SetBoldMode(int Sequence, bool IsEnabled) : NonPrintingElement(Sequence);
