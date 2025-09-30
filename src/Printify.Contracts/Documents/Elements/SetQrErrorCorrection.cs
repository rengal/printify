namespace Printify.Contracts.Documents.Elements;

/// <summary>
/// Selects the QR error correction level for subsequent symbols via GS ( k.
/// </summary>
/// <param name=\"Sequence\">Monotonic sequence index within the document stream.</param>
/// <param name=\"Level\">Chosen QR error correction level.</param>
public sealed record SetQrErrorCorrection(int Sequence, QrErrorCorrectionLevel Level) : NonPrintingElement(Sequence);
