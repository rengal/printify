namespace Printify.Contracts.Layout;

// Renderer-computed placement and stacking info for a specific element.
public sealed record ElementMetrics(
    int Sequence,
    Rect Box,
    int LineIndex = -1,
    int ZIndex = 0,
    Anchor Anchor = Anchor.None
);

