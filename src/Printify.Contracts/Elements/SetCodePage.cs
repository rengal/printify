namespace Printify.Contracts.Elements;

/// <summary>
/// Sets the code page used to decode incoming bytes to text.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="CodePage">Code page identifier/name (e.g., "CP437", "CP850").</param>
public sealed record SetCodePage(int Sequence, string CodePage) : NonPrintingElement(Sequence);

