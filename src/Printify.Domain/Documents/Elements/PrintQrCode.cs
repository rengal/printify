namespace Printify.Domain.Documents.Elements;

/// <summary>
/// Emits a QR code print request using the last stored payload.
/// </summary>
/// <param name=\"Sequence\">Monotonic sequence index within the document stream.</param>
/// <param name=\"Content\">Payload rendered by the QR symbol.</param>
public sealed record PrintQrCode(int Sequence, string Content) : PrintingElement(Sequence);
