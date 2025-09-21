namespace Printify.Contracts.Elements;

/// <summary>
/// Enables or disables reverse (white-on-black) print mode (GS B).
/// </summary>
/// <param name=\"Sequence\">Monotonic sequence index within the document stream.</param>
/// <param name=\"IsEnabled\">True when reverse mode is turned on; false when turned off.</param>
public sealed record SetReverseMode(int Sequence, bool IsEnabled) : NonPrintingElement(Sequence);
