using Printify.Web.Contracts.Documents.Responses.Canvas.Elements;

namespace Printify.Web.Contracts.Documents.Responses.Canvas;

/// <summary>
/// Canvas representation with dimensions and layout items.
/// </summary>
/// <param name="WidthInDots">Canvas width in dots.</param>
/// <param name="HeightInDots">Optional canvas height in dots.</param>
/// <param name="Items">Ordered list of canvas primitives and debug entries.</param>
public sealed record CanvasDto(
    int WidthInDots,
    int? HeightInDots,
    IReadOnlyList<CanvasElementDto> Items);
