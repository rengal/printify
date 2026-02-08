namespace Printify.Infrastructure.Persistence.Entities.Documents.Epl;

/// <summary>
/// Defines the discriminators used to serialize EPL document elements in storage.
/// </summary>
internal static class EplDocumentElementTypeNames
{
    public const string ClearBuffer = "eplClearBuffer";
    public const string CarriageReturn = "eplCarriageReturn";
    public const string LineFeed = "eplLineFeed";
    public const string SetLabelWidth = "eplSetLabelWidth";
    public const string SetLabelHeight = "eplSetLabelHeight";
    public const string SetPrintSpeed = "eplSetPrintSpeed";
    public const string SetPrintDarkness = "eplSetPrintDarkness";
    public const string SetPrintDirection = "eplSetPrintDirection";
    public const string SetInternationalCharacter = "eplSetInternationalCharacter";
    public const string PrinterError = "eplPrinterError";
    public const string ScalableText = "eplScalableText";
    public const string DrawHorizontalLine = "eplDrawHorizontalLine";
    public const string DrawLine = "eplDrawLine";
    public const string Print = "eplPrint";
    public const string PrintBarcode = "eplPrintBarcode";
    public const string EplRasterImage = "eplRasterImage";
}
