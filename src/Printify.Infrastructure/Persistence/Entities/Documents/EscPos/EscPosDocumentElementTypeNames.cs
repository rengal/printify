namespace Printify.Infrastructure.Persistence.Entities.Documents.EscPos;

/// <summary>
/// Defines the discriminators used to serialize ESC/POS document elements in storage.
/// </summary>
internal static class EscPosDocumentElementTypeNames
{
    public const string Bell = "bell";
    public const string Error = "error";
    public const string Pagecut = "pagecut";
    public const string PrinterError = "printerError";
    public const string PrinterStatus = "printerStatus";
    public const string PrintBarcode = "printBarcode";
    public const string PrintQrCode = "printQrCode";
    public const string Pulse = "pulse";
    public const string ResetPrinter = "resetPrinter";
    public const string SetBarcodeHeight = "setBarcodeHeight";
    public const string SetBarcodeLabelPosition = "setBarcodeLabelPosition";
    public const string SetBarcodeModuleWidth = "setBarcodeModuleWidth";
    public const string SetBoldMode = "setBoldMode";
    public const string CancelBoldMode = "cancelBoldMode";
    public const string SetDoubleStrikeMode = "setDoubleStrikeMode";
    public const string SetCodePage = "setCodePage";
    public const string SetPrintMode = "setPrintMode";
    public const string SetCharacterSize = "setCharacterSize";
    public const string SetRightCharacterSpacing = "setRightCharacterSpacing";
    public const string SetUpsideDownMode = "setUpsideDownMode";
    public const string SetJustification = "setJustification";
    public const string SetLineSpacing = "setLineSpacing";
    public const string ResetLineSpacing = "resetLineSpacing";
    public const string SetQrErrorCorrection = "setQrErrorCorrection";
    public const string SetQrModel = "setQrModel";
    public const string SetQrModuleSize = "setQrModuleSize";
    public const string SetReverseMode = "setReverseMode";
    public const string EnableItalicMode = "enableItalicMode";
    public const string DisableItalicMode = "disableItalicMode";
    public const string SetUnderlineMode = "setUnderlineMode";
    public const string StoreQrData = "storeQrData";
    public const string StoredLogo = "storedLogo";
    public const string AppendToLineBuffer = "appendToLineBuffer";
    public const string FlushLineBufferAndFeed = "flushLineBufferAndFeed";
    public const string LegacyCarriageReturn = "legacyCarriageReturn";
    public const string RasterImage = "rasterImage";
    public const string StatusRequest = "statusRequest";
    public const string StatusResponse = "statusResponse";
    public const string SetFont = "setFont";
    public const string PrintAndFeedLines = "printAndFeedLines";
    public const string PrintAndFeedDots = "printAndFeedDots";
    public const string HorizontalTab = "horizontalTab";
    public const string SetHorizontalTabStops = "setHorizontalTabStops";
}
