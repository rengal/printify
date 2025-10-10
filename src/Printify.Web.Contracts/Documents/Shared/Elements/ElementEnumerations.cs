namespace Printify.Web.Contracts.Documents.Shared.Elements;

/// <summary>
/// Label (HRI) positioning options for barcodes driven by GS H (0x1D 0x48).
/// </summary>
public enum BarcodeLabelPosition
{
    /// <summary>
    /// Suppress human-readable interpretation text output.
    /// </summary>
    NotPrinted,

    /// <summary>
    /// Render label above the barcode.
    /// </summary>
    Above,

    /// <summary>
    /// Render label below the barcode.
    /// </summary>
    Below,

    /// <summary>
    /// Render label both above and below the barcode.
    /// </summary>
    AboveAndBelow
}

/// <summary>
/// Barcode symbologies supported across printer protocols.
/// </summary>
public enum BarcodeSymbology
{
    /// <summary>
    /// US UPC-A.
    /// </summary>
    UpcA,

    /// <summary>
    /// US UPC-E.
    /// </summary>
    UpcE,

    /// <summary>
    /// JAN (EAN) 13-digit barcode.
    /// </summary>
    Ean13,

    /// <summary>
    /// JAN (EAN) 8-digit barcode.
    /// </summary>
    Ean8,

    /// <summary>
    /// Code 39 symbology.
    /// </summary>
    Code39,

    /// <summary>
    /// Interleaved 2 of 5.
    /// </summary>
    Itf,

    /// <summary>
    /// Codabar (NW-7).
    /// </summary>
    Codabar,

    /// <summary>
    /// Code 93 symbology.
    /// </summary>
    Code93,

    /// <summary>
    /// Code 128 symbology.
    /// </summary>
    Code128
}

/// <summary>
/// Canonical string tokens for barcode symbologies exposed via the web API.
/// </summary>
public static class BarcodeLabelPositionNames
{
    public const string NotPrinted = "notPrinted";
    public const string Above = "above";
    public const string Below = "below";
    public const string AboveAndBelow = "aboveAndBelow";
}

/// <summary>
/// Canonical string tokens for barcode symbologies exposed via the web API.
/// </summary>
public static class BarcodeSymbologyNames
{
    public const string UpcA = "upcA";
    public const string UpcE = "upcE";
    public const string Ean13 = "ean13";
    public const string Ean8 = "ean8";
    public const string Code39 = "code39";
    public const string Itf = "itf";
    public const string Codabar = "codabar";
    public const string Code93 = "code93";
    public const string Code128 = "code128";
}

/// <summary>
/// Reason for stream completion.
/// </summary>
public enum CompletionReason
{
    ClientDisconnected,
    DataTimeout
}

/// <summary>
/// Cash drawer pin selection for pulse commands.
/// </summary>
public enum PulsePin
{
    /// <summary>Drawer 1 pin.</summary>
    Drawer1,
    /// <summary>Drawer 2 pin.</summary>
    Drawer2
}

/// <summary>
/// Error correction levels supported by QR codes in ESC/POS.
/// </summary>
public enum QrErrorCorrectionLevel
{
    /// <summary>
    /// Level L (7% recovery).
    /// </summary>
    Low,

    /// <summary>
    /// Level M (15% recovery).
    /// </summary>
    Medium,

    /// <summary>
    /// Level Q (25% recovery).
    /// </summary>
    Quartile,

    /// <summary>
    /// Level H (30% recovery).
    /// </summary>
    High
}

/// <summary>
/// Supported QR code models for ESC/POS GS ( k configuration.
/// </summary>
public enum QrModel
{
    /// <summary>
    /// Model 1 QR symbol.
    /// </summary>
    Model1,

    /// <summary>
    /// Model 2 QR symbol (most common).
    /// </summary>
    Model2,

    /// <summary>
    /// Micro QR symbol.
    /// </summary>
    Micro
}

/// <summary>
/// Supported text alignment values for ESC a (justification) commands.
/// </summary>
public enum TextJustification
{
    /// <summary>
    /// Align text to the left margin.
    /// </summary>
    Left,

    /// <summary>
    /// Center text relative to the printable width.
    /// </summary>
    Center,

    /// <summary>
    /// Align text to the right margin.
    /// </summary>
    Right
}

/// <summary>
/// Canonical string tokens for completion reasons exposed via the web API.
/// </summary>
public static class CompletionReasonNames
{
    public const string ClientDisconnected = "clientDisconnected";
    public const string DataTimeout = "dataTimeout";
}

/// <summary>
/// Canonical string tokens for pulse pin selection.
/// </summary>
public static class PulsePinNames
{
    public const string Drawer1 = "drawer1";
    public const string Drawer2 = "drawer2";
}

/// <summary>
/// Canonical string tokens for QR error correction levels.
/// </summary>
public static class QrErrorCorrectionLevelNames
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string Quartile = "quartile";
    public const string High = "high";
}

/// <summary>
/// Canonical string tokens for QR code model selection.
/// </summary>
public static class QrModelNames
{
    public const string Model1 = "model1";
    public const string Model2 = "model2";
    public const string Micro = "micro";
}

/// <summary>
/// Canonical string tokens for text justification values.
/// </summary>
public static class TextJustificationNames
{
    public const string Left = "left";
    public const string Center = "center";
    public const string Right = "right";
}
