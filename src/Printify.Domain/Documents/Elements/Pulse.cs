namespace Printify.Domain.Documents.Elements;

/// <summary>
/// A cash drawer pulse signal sent to a specific pin.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Pin">Target drawer pin (e.g., Drawer1/Drawer2).</param>
/// <param name="OnTimeMs">Pulse ON interval in milliseconds.</param>
/// <param name="OffTimeMs">Pulse OFF interval in milliseconds.</param>
public sealed record Pulse(int Sequence, PulsePin Pin, int OnTimeMs, int OffTimeMs)
    : NonPrintingElement(Sequence);
