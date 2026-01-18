namespace Printify.Infrastructure.Persistence.Entities.Documents.EscPos;

using System.Text.Json.Serialization;

/// <summary>
/// Base class for all ESC/POS document element payloads.
/// </summary>
public abstract record EscPosDocumentElementPayload;

// ESC/POS Command Payloads

public sealed record BellElementPayload : EscPosDocumentElementPayload;

public sealed record ErrorElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Code,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Message)
    : EscPosDocumentElementPayload;

public sealed record PagecutElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Mode,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? FeedMotionUnits)
    : EscPosDocumentElementPayload;

public sealed record PrinterErrorElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Message)
    : EscPosDocumentElementPayload;

public sealed record PrinterStatusElementPayload(
    byte StatusByte,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    byte? AdditionalStatusByte)
    : EscPosDocumentElementPayload;

public sealed record PrintBarcodeElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Symbology,
    string Data,
    int Width,
    int Height,
    Guid MediaId)
    : EscPosDocumentElementPayload;

public sealed record PrintQrCodeElementPayload(
    string Data,
    int Width,
    int Height,
    Guid MediaId)
    : EscPosDocumentElementPayload;

public sealed record RasterImageElementPayload(
    int Width,
    int Height,
    Guid MediaId) : EscPosDocumentElementPayload;

public sealed record PulseElementPayload(
    int Pin,
    int OnTimeMs,
    int OffTimeMs)
    : EscPosDocumentElementPayload;

public sealed record ResetPrinterElementPayload : EscPosDocumentElementPayload;

public sealed record SetBarcodeHeightElementPayload(int HeightInDots) : EscPosDocumentElementPayload;

public sealed record SetBarcodeLabelPositionElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Position)
    : EscPosDocumentElementPayload;

public sealed record SetBarcodeModuleWidthElementPayload(int ModuleWidth) : EscPosDocumentElementPayload;

public sealed record SetBoldModeElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsEnabled)
    : EscPosDocumentElementPayload;

public sealed record SetCodePageElementPayload(string CodePage) : EscPosDocumentElementPayload;

public sealed record SetFontElementPayload(
    int FontNumber,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsDoubleWidth,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsDoubleHeight)
    : EscPosDocumentElementPayload;

public sealed record SetJustificationElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Justification)
    : EscPosDocumentElementPayload;

public sealed record SetLineSpacingElementPayload(int Spacing) : EscPosDocumentElementPayload;

public sealed record ResetLineSpacingElementPayload : EscPosDocumentElementPayload;

public sealed record SetQrErrorCorrectionElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Level)
    : EscPosDocumentElementPayload;

public sealed record SetQrModelElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Model)
    : EscPosDocumentElementPayload;

public sealed record SetQrModuleSizeElementPayload(int ModuleSize) : EscPosDocumentElementPayload;

public sealed record SetReverseModeElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsEnabled)
    : EscPosDocumentElementPayload;

public sealed record SetUnderlineModeElementPayload(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? IsEnabled)
    : EscPosDocumentElementPayload;

public sealed record StoreQrDataElementPayload(string Content) : EscPosDocumentElementPayload;

public sealed record StoredLogoElementPayload(int LogoId) : EscPosDocumentElementPayload;

public sealed record AppendToLineBufferElementPayload : EscPosDocumentElementPayload
{
    [JsonPropertyName("rawBytes")]
    public string RawBytesHex { get; init; } = string.Empty;
}

public sealed record FlushLineBufferAndFeedElementPayload : EscPosDocumentElementPayload;

public sealed record LegacyCarriageReturnElementPayload : EscPosDocumentElementPayload;

public sealed record StatusRequestElementPayload(byte RequestType) : EscPosDocumentElementPayload;

public sealed record StatusResponseElementPayload(
    byte StatusByte,
    bool IsPaperOut,
    bool IsCoverOpen,
    bool IsOffline) : EscPosDocumentElementPayload;
