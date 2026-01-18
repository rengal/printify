namespace Printify.Infrastructure.Persistence.Entities.Documents.Epl;

/// <summary>
/// Defines the discriminators used to serialize EPL document elements in storage.
/// </summary>
internal static class EplDocumentElementTypeNames
{
    public const string ClearBuffer = "eplClearBuffer";
    public const string SetLabelWidth = "eplSetLabelWidth";
    public const string SetLabelHeight = "eplSetLabelHeight";
    public const string SetPrintSpeed = "eplSetPrintSpeed";
    public const string SetPrintDarkness = "eplSetPrintDarkness";
    public const string SetPrintDirection = "eplSetPrintDirection";
    public const string SetInternationalCharacter = "eplSetInternationalCharacter";
    public const string SetCodePage = "eplSetCodePage";
    public const string ScalableText = "eplScalableText";
    public const string DrawHorizontalLine = "eplDrawHorizontalLine";
    public const string DrawLine = "eplDrawLine";
    public const string Print = "eplPrint";
    public const string PrintBarcode = "eplPrintBarcode";
    public const string PrintGraphic = "eplPrintGraphic";
}
