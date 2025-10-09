namespace Printify.Web.Contracts.Documents.Elements;

/// <summary>
/// A paper cut operation (full or partial depending on command parsed).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public sealed record Pagecut(int Sequence) : NonPrintingElement(Sequence);
