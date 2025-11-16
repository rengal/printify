namespace Printify.Infrastructure.Documents;

using System.Text.Json.Serialization;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(BellElementDto), "bell")]
[JsonDerivedType(typeof(ErrorElementDto), "error")]
[JsonDerivedType(typeof(PagecutElementDto), "pagecut")]
[JsonDerivedType(typeof(PrinterErrorElementDto), "printerError")]
[JsonDerivedType(typeof(PrinterStatusElementDto), "printerStatus")]
[JsonDerivedType(typeof(PrintBarcodeElementDto), "printBarcode")]
[JsonDerivedType(typeof(PrintQrCodeElementDto), "printQrCode")]
[JsonDerivedType(typeof(PulseElementDto), "pulse")]
[JsonDerivedType(typeof(ResetPrinterElementDto), "resetPrinter")]
[JsonDerivedType(typeof(SetBarcodeHeightElementDto), "setBarcodeHeight")]
[JsonDerivedType(typeof(SetBarcodeLabelPositionElementDto), "setBarcodeLabelPosition")]
[JsonDerivedType(typeof(SetBarcodeModuleWidthElementDto), "setBarcodeModuleWidth")]
[JsonDerivedType(typeof(SetBoldModeElementDto), "setBoldMode")]
[JsonDerivedType(typeof(SetCodePageElementDto), "setCodePage")]
[JsonDerivedType(typeof(SetFontElementDto), "setFont")]
[JsonDerivedType(typeof(SetJustificationElementDto), "setJustification")]
[JsonDerivedType(typeof(SetLineSpacingElementDto), "setLineSpacing")]
[JsonDerivedType(typeof(ResetLineSpacingElementDto), "resetLineSpacing")]
[JsonDerivedType(typeof(SetQrErrorCorrectionElementDto), "setQrErrorCorrection")]
[JsonDerivedType(typeof(SetQrModelElementDto), "setQrModel")]
[JsonDerivedType(typeof(SetQrModuleSizeElementDto), "setQrModuleSize")]
[JsonDerivedType(typeof(SetReverseModeElementDto), "setReverseMode")]
[JsonDerivedType(typeof(SetUnderlineModeElementDto), "setUnderlineMode")]
[JsonDerivedType(typeof(StoreQrDataElementDto), "storeQrData")]
[JsonDerivedType(typeof(StoredLogoElementDto), "storedLogo")]
[JsonDerivedType(typeof(TextLineElementDto), "textLine")]
public abstract record DocumentElementDto;

public sealed record BellElementDto : DocumentElementDto;

public sealed record ErrorElementDto(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Code,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Message)
    : DocumentElementDto;

public sealed record PagecutElementDto(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Mode,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? FeedMotionUnits)
    : DocumentElementDto;

public sealed record PrinterErrorElementDto(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Message)
    : DocumentElementDto;

public sealed record PrinterStatusElementDto(
    byte StatusByte,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    byte? AdditionalStatusByte)
    : DocumentElementDto;

public sealed record PrintBarcodeElementDto(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Symbology,
    string Data)
    : DocumentElementDto;

public sealed record PrintQrCodeElementDto : DocumentElementDto;

public sealed record PulseElementDto(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Pin,
    int OnTimeMs,
    int OffTimeMs)
    : DocumentElementDto;

public sealed record ResetPrinterElementDto : DocumentElementDto;

public sealed record SetBarcodeHeightElementDto(int HeightInDots) : DocumentElementDto;

public sealed record SetBarcodeLabelPositionElementDto(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Position)
    : DocumentElementDto;

public sealed record SetBarcodeModuleWidthElementDto(int ModuleWidth) : DocumentElementDto;

public sealed record SetBoldModeElementDto(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsEnabled)
    : DocumentElementDto;

public sealed record SetCodePageElementDto(string CodePage) : DocumentElementDto;

public sealed record SetFontElementDto(
    int FontNumber,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsDoubleWidth,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsDoubleHeight)
    : DocumentElementDto;

public sealed record SetJustificationElementDto(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Justification)
    : DocumentElementDto;

public sealed record SetLineSpacingElementDto(int Spacing) : DocumentElementDto;

public sealed record ResetLineSpacingElementDto : DocumentElementDto;

public sealed record SetQrErrorCorrectionElementDto(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Level)
    : DocumentElementDto;

public sealed record SetQrModelElementDto(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Model)
    : DocumentElementDto;

public sealed record SetQrModuleSizeElementDto(int ModuleSize) : DocumentElementDto;

public sealed record SetReverseModeElementDto(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsEnabled)
    : DocumentElementDto;

public sealed record SetUnderlineModeElementDto(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsEnabled)
    : DocumentElementDto;

public sealed record StoreQrDataElementDto(string Content) : DocumentElementDto;

public sealed record StoredLogoElementDto(int LogoId) : DocumentElementDto;

public sealed record TextLineElementDto(string Text) : DocumentElementDto;
