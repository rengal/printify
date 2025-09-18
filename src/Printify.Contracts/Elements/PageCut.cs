namespace Printify.Contracts.Elements;

/// <summary>
/// A paper cut operation (full or partial depending on command parsed).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public sealed record PageCut(int Sequence) : NonPrintingElement(Sequence);
