namespace Printify.Infrastructure.Documents;

/// <summary>
/// Defines the discriminators used to serialize document elements in storage.
/// </summary>
internal static class DocumentElementTypeNames
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
    public const string SetCodePage = "setCodePage";
    public const string SetFont = "setFont";
    public const string SetJustification = "setJustification";
    public const string SetLineSpacing = "setLineSpacing";
    public const string ResetLineSpacing = "resetLineSpacing";
    public const string SetQrErrorCorrection = "setQrErrorCorrection";
    public const string SetQrModel = "setQrModel";
    public const string SetQrModuleSize = "setQrModuleSize";
    public const string SetReverseMode = "setReverseMode";
    public const string SetUnderlineMode = "setUnderlineMode";
    public const string StoreQrData = "storeQrData";
    public const string StoredLogo = "storedLogo";
    public const string AppendToLineBuffer = "appendToLineBuffer";
    public const string FlushLineBufferAndFeed = "flushLineBufferAndFeed";
    public const string RasterImage = "rasterImage";
}
