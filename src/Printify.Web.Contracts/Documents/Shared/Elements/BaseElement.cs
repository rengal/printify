namespace Printify.Web.Contracts.Documents.Shared.Elements;

/// <summary>
/// Base contract for all document elements with sequencing metadata.
/// </summary>
public abstract record BaseElement;

/// <summary>
/// Marker base for elements that produce visible output during printing.
/// </summary>
public abstract record PrintingElement : BaseElement;

/// <summary>
/// Marker base for elements that modify state or encode diagnostics without rendering.
/// </summary>
public abstract record NonPrintingElement : BaseElement;
