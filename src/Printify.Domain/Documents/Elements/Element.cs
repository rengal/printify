using System.Text.Json.Serialization;

namespace Printify.Domain.Documents.Elements;

/// <summary>
/// Base type for all document elements produced by tokenizers and consumed by renderers.
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "")]
[JsonDerivedType(typeof(Bell), "bell")]
[JsonDerivedType(typeof(Error), "error")]
[JsonDerivedType(typeof(Pagecut), "pagecut")]
[JsonDerivedType(typeof(PrinterError), "printerError")]
[JsonDerivedType(typeof(PrinterStatus), "printerStatus")]
[JsonDerivedType(typeof(PrintBarcode), "printBarcode")]
[JsonDerivedType(typeof(PrintQrCode), "printQrCode")]
[JsonDerivedType(typeof(Pulse), "pulse")]
[JsonDerivedType(typeof(RasterImageContent), "rasterImageContent")]
[JsonDerivedType(typeof(RasterImageDescriptor), "rasterImageDescriptor")]
[JsonDerivedType(typeof(ResetPrinter), "resetPrinter")]
[JsonDerivedType(typeof(SetBarcodeHeight), "setBarcodeHeight")]
[JsonDerivedType(typeof(SetBarcodeLabelPosition), "setBarcodeLabelPosition")]
[JsonDerivedType(typeof(SetBarcodeModuleWidth), "setBarcodeModuleWidth")]
[JsonDerivedType(typeof(SetBoldMode), "setBoldMode")]
[JsonDerivedType(typeof(SetCodePage), "setCodePage")]
[JsonDerivedType(typeof(SetFont), "setFont")]
[JsonDerivedType(typeof(SetJustification), "setJustification")]
[JsonDerivedType(typeof(SetLineSpacing), "setLineSpacing")]
[JsonDerivedType(typeof(SetQrErrorCorrection), "setQrErrorCorrection")]
[JsonDerivedType(typeof(SetQrModel), "setQrModel")]
[JsonDerivedType(typeof(SetQrModuleSize), "setQrModuleSize")]
[JsonDerivedType(typeof(SetReverseMode), "setReverseMode")]
[JsonDerivedType(typeof(SetUnderlineMode), "setUnderlineMode")]
[JsonDerivedType(typeof(StoreQrData), "storeQrData")]
[JsonDerivedType(typeof(StoredLogo), "storedLogo")]
[JsonDerivedType(typeof(TextLine), "textLine")]
public abstract record Element(int Sequence);
