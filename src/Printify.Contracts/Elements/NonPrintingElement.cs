namespace Printify.Contracts.Elements;

/// <summary>
/// Base type for non-printing control or status events within a document stream.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public abstract record NonPrintingElement(int Sequence) : Element(Sequence);
