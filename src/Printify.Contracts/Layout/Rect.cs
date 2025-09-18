namespace Printify.Contracts.Layout;

// Canonical content box for an element, in printer dots.
public readonly record struct Rect(int X, int Y, int Width, int Height);

