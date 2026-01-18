using Printify.Domain.Layout.Primitives;

namespace Printify.Domain.Layout;

/// <summary>
/// Protocol-agnostic rendering canvas containing ordered layout items.
/// Represents the output of rendering protocol commands into a common visual format.
/// Items are ordered as they appear in the document - debug info and visual primitives are mixed.
/// </summary>
/// <param name="WidthInDots">Canvas width in dots (defines X coordinate boundary).</param>
/// <param name="HeightInDots">Canvas height in dots (defines Y coordinate boundary). Null for continuous roll.</param>
/// <param name="Items">Ordered list of layout items (visual primitives and debug info).</param>
public sealed record Canvas(
    int WidthInDots,
    int? HeightInDots,
    IReadOnlyList<BaseElement> Items);
