namespace Printify.Contracts.Elements;

/// <summary>
/// Base type for all document elements produced by tokenizers and consumed by renderers.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public abstract record Element(int Sequence);
