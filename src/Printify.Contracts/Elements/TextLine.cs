namespace Printify.Contracts.Elements;

/// <summary>
/// A printable line of text emitted by the printer protocol.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Text">Raw text content (decoded as parsed; typically ASCII/CP437 in MVP).</param>
public sealed record TextLine(int Sequence, string Text) : PrintingElement(Sequence);
