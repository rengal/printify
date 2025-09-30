namespace Printify.Contracts.Documents.Elements;

/// <summary>
/// Configures the QR code model for subsequent GS ( k sequences.
/// </summary>
/// <param name=\"Sequence\">Monotonic sequence index within the document stream.</param>
/// <param name=\"Model\">Selected QR code model.</param>
public sealed record SetQrModel(int Sequence, QrModel Model) : NonPrintingElement(Sequence);
