namespace Printify.Contracts.Documents.Elements;

/// <summary>
/// Supported QR code models for ESC/POS GS ( k configuration.
/// </summary>
public enum QrModel
{
    /// <summary>
    /// Model 1 QR symbol.
    /// </summary>
    Model1 = 1,

    /// <summary>
    /// Model 2 QR symbol (most common).
    /// </summary>
    Model2 = 2,

    /// <summary>
    /// Micro QR symbol.
    /// </summary>
    Micro = 3
}
