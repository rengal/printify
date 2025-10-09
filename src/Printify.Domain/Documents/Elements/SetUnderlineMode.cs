namespace Printify.Domain.Documents.Elements;

/// <summary>
/// Enables or disables underline text mode (ESC -).
/// </summary>
/// <param name=\"Sequence\">Monotonic sequence index within the document stream.</param>
/// <param name=\"IsEnabled\">True when underline mode is turned on; false when turned off.</param>
public sealed record SetUnderlineMode(int Sequence, bool IsEnabled) : NonPrintingElement(Sequence);
