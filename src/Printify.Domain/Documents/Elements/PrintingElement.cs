namespace Printify.Domain.Documents.Elements;

/// <summary>
/// Base type for printing (visible) elements that produce output on paper.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public abstract record PrintingElement(int Sequence) : Element(Sequence);

