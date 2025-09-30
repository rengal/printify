namespace Printify.Contracts.Documents.Elements;

/// <summary>
/// Stores QR code data into the printer memory using GS ( k.
/// </summary>
/// <param name=\"Sequence\">Monotonic sequence index within the document stream.</param>
/// <param name=\"Content\">Payload to be encoded into the QR symbol.</param>
public sealed record StoreQrData(int Sequence, string Content) : NonPrintingElement(Sequence);
