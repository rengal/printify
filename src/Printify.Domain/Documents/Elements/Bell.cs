namespace Printify.Domain.Documents.Elements;

/// <summary>
/// An audible/attention bell signal.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public sealed record Bell(int Sequence) : NonPrintingElement(Sequence);
