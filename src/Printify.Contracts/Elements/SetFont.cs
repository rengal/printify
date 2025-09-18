namespace Printify.Contracts.Elements;

/// <summary>
/// Changes the active font selection for subsequent printed text using ESC ! (0x1B 0x21) semantics.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="FontNumber">Protocol-specific font number (e.g., 0=A, 1=B).</param>
/// <param name="IsDoubleWidth">True when double-width bit is set.</param>
/// <param name="IsDoubleHeight">True when double-height bit is set.</param>
public sealed record SetFont(int Sequence, int FontNumber, bool IsDoubleWidth, bool IsDoubleHeight)
    : NonPrintingElement(Sequence);
