namespace Printify.Web.Contracts.Documents.Shared.Elements;

/// <summary>
/// Base contract for all document elements with sequencing metadata.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public abstract record BaseElement(int Sequence);

/// <summary>
/// Marker base for elements that produce visible output during printing.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public abstract record PrintingElement(int Sequence) : BaseElement(Sequence);

/// <summary>
/// Marker base for elements that modify state or encode diagnostics without rendering.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
public abstract record NonPrintingElement(int Sequence) : BaseElement(Sequence);
