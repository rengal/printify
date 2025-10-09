namespace Printify.Domain.Documents.Elements;

/// <summary>
/// Error correction levels supported by QR codes in ESC/POS.
/// </summary>
public enum QrErrorCorrectionLevel
{
    /// <summary>
    /// Level L (7% recovery).
    /// </summary>
    Low = 0,

    /// <summary>
    /// Level M (15% recovery).
    /// </summary>
    Medium = 1,

    /// <summary>
    /// Level Q (25% recovery).
    /// </summary>
    Quartile = 2,

    /// <summary>
    /// Level H (30% recovery).
    /// </summary>
    High = 3
}
