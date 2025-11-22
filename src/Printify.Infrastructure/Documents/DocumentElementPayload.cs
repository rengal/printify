namespace Printify.Infrastructure.Documents;

using System.Text.Json.Serialization;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(BellElementPayload), DocumentElementTypeNames.Bell)]
[JsonDerivedType(typeof(ErrorElementPayload), DocumentElementTypeNames.Error)]
[JsonDerivedType(typeof(PagecutElementPayload), DocumentElementTypeNames.Pagecut)]
[JsonDerivedType(typeof(PrinterErrorElementPayload), DocumentElementTypeNames.PrinterError)]
[JsonDerivedType(typeof(PrinterStatusElementPayload), DocumentElementTypeNames.PrinterStatus)]
[JsonDerivedType(typeof(PrintBarcodeElementPayload), DocumentElementTypeNames.PrintBarcode)]
[JsonDerivedType(typeof(PrintQrCodeElementPayload), DocumentElementTypeNames.PrintQrCode)]
[JsonDerivedType(typeof(PulseElementPayload), DocumentElementTypeNames.Pulse)]
[JsonDerivedType(typeof(ResetPrinterElementPayload), DocumentElementTypeNames.ResetPrinter)]
[JsonDerivedType(typeof(SetBarcodeHeightElementPayload), DocumentElementTypeNames.SetBarcodeHeight)]
[JsonDerivedType(typeof(SetBarcodeLabelPositionElementPayload), DocumentElementTypeNames.SetBarcodeLabelPosition)]
[JsonDerivedType(typeof(SetBarcodeModuleWidthElementPayload), DocumentElementTypeNames.SetBarcodeModuleWidth)]
[JsonDerivedType(typeof(SetBoldModeElementPayload), DocumentElementTypeNames.SetBoldMode)]
[JsonDerivedType(typeof(SetCodePageElementPayload), DocumentElementTypeNames.SetCodePage)]
[JsonDerivedType(typeof(SetFontElementPayload), DocumentElementTypeNames.SetFont)]
[JsonDerivedType(typeof(SetJustificationElementPayload), DocumentElementTypeNames.SetJustification)]
[JsonDerivedType(typeof(SetLineSpacingElementPayload), DocumentElementTypeNames.SetLineSpacing)]
[JsonDerivedType(typeof(ResetLineSpacingElementPayload), DocumentElementTypeNames.ResetLineSpacing)]
[JsonDerivedType(typeof(SetQrErrorCorrectionElementPayload), DocumentElementTypeNames.SetQrErrorCorrection)]
[JsonDerivedType(typeof(SetQrModelElementPayload), DocumentElementTypeNames.SetQrModel)]
[JsonDerivedType(typeof(SetQrModuleSizeElementPayload), DocumentElementTypeNames.SetQrModuleSize)]
[JsonDerivedType(typeof(SetReverseModeElementPayload), DocumentElementTypeNames.SetReverseMode)]
[JsonDerivedType(typeof(SetUnderlineModeElementPayload), DocumentElementTypeNames.SetUnderlineMode)]
[JsonDerivedType(typeof(StoreQrDataElementPayload), DocumentElementTypeNames.StoreQrData)]
[JsonDerivedType(typeof(StoredLogoElementPayload), DocumentElementTypeNames.StoredLogo)]
[JsonDerivedType(typeof(TextLineElementPayload), DocumentElementTypeNames.TextLine)]
[JsonDerivedType(typeof(RasterImageElementPayload), DocumentElementTypeNames.RasterImage)]
public abstract record DocumentElementPayload;

public sealed record BellElementPayload : DocumentElementPayload;

public sealed record ErrorElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Code,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Message)
    : DocumentElementPayload;

public sealed record PagecutElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Mode,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? FeedMotionUnits)
    : DocumentElementPayload;

public sealed record PrinterErrorElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Message)
    : DocumentElementPayload;

public sealed record PrinterStatusElementPayload(
    byte StatusByte,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    byte? AdditionalStatusByte)
    : DocumentElementPayload;

public sealed record PrintBarcodeElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Symbology,
    string Data)
    : DocumentElementPayload;

public sealed record PrintQrCodeElementPayload : DocumentElementPayload;

public sealed record PulseElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Pin,
    int OnTimeMs,
    int OffTimeMs)
    : DocumentElementPayload;

public sealed record ResetPrinterElementPayload : DocumentElementPayload;

public sealed record SetBarcodeHeightElementPayload(int HeightInDots) : DocumentElementPayload;

public sealed record SetBarcodeLabelPositionElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Position)
    : DocumentElementPayload;

public sealed record SetBarcodeModuleWidthElementPayload(int ModuleWidth) : DocumentElementPayload;

public sealed record SetBoldModeElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsEnabled)
    : DocumentElementPayload;

public sealed record SetCodePageElementPayload(string CodePage) : DocumentElementPayload;

public sealed record SetFontElementPayload(
    int FontNumber,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsDoubleWidth,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsDoubleHeight)
    : DocumentElementPayload;

public sealed record SetJustificationElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Justification)
    : DocumentElementPayload;

public sealed record SetLineSpacingElementPayload(int Spacing) : DocumentElementPayload;

public sealed record ResetLineSpacingElementPayload : DocumentElementPayload;

public sealed record SetQrErrorCorrectionElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Level)
    : DocumentElementPayload;

public sealed record SetQrModelElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Model)
    : DocumentElementPayload;

public sealed record SetQrModuleSizeElementPayload(int ModuleSize) : DocumentElementPayload;

public sealed record SetReverseModeElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsEnabled)
    : DocumentElementPayload;

public sealed record SetUnderlineModeElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsEnabled)
    : DocumentElementPayload;

public sealed record StoreQrDataElementPayload(string Content) : DocumentElementPayload;

public sealed record StoredLogoElementPayload(int LogoId) : DocumentElementPayload;

public sealed record TextLineElementPayload(string Text) : DocumentElementPayload;

public sealed record RasterImageElementPayload(int Width, int Height) : DocumentElementPayload;
