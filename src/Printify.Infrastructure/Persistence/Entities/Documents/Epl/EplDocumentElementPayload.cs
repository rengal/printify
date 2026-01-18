namespace Printify.Infrastructure.Persistence.Entities.Documents.Epl;

/// <summary>
/// Base class for all EPL document element payloads.
/// </summary>
public abstract record EplDocumentElementPayload;

// EPL Command Payloads

public sealed record ClearBufferElementPayload : EplDocumentElementPayload;

public sealed record SetLabelWidthElementPayload(int Width) : EplDocumentElementPayload;

public sealed record SetLabelHeightElementPayload(
    int Height,
    int SecondParameter) : EplDocumentElementPayload;

public sealed record SetPrintSpeedElementPayload(int Speed) : EplDocumentElementPayload;

public sealed record SetPrintDarknessElementPayload(int Darkness) : EplDocumentElementPayload;

public sealed record SetPrintDirectionElementPayload(string Direction) : EplDocumentElementPayload;

public sealed record SetInternationalCharacterElementPayload(int Code) : EplDocumentElementPayload;

public sealed record SetCodePageElementPayload(
    int Code,
    int Scaling) : EplDocumentElementPayload;

public sealed record ScalableTextElementPayload(
    int X,
    int Y,
    int Rotation,
    int Font,
    int HorizontalMultiplication,
    int VerticalMultiplication,
    string Reverse,
    string TextBytesHex) : EplDocumentElementPayload;

public sealed record DrawHorizontalLineElementPayload(
    int X,
    int Y,
    int Thickness,
    int Length) : EplDocumentElementPayload;

public sealed record DrawLineElementPayload(
    int X1,
    int Y1,
    int Thickness,
    int X2,
    int Y2) : EplDocumentElementPayload;

public sealed record PrintElementPayload(int Copies) : EplDocumentElementPayload;

public sealed record PrintBarcodeElementPayload(
    int X,
    int Y,
    int Rotation,
    string Type,
    int Width,
    int Height,
    string Hri,
    string Data) : EplDocumentElementPayload;

public sealed record PrintGraphicElementPayload(
    int X,
    int Y,
    int Width,
    int Height,
    string DataHex) : EplDocumentElementPayload;
