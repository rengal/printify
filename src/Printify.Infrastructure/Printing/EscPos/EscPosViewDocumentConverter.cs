using Printify.Application.Exceptions;
using Printify.Application.Features.Printers.Documents;
using Printify.Application.Features.Printers.Documents.View;
using Printify.Domain.Documents;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Documents.View;
using Printify.Domain.Mapping;
using Printify.Domain.Printers;

namespace Printify.Infrastructure.Printing.EscPos;

public sealed class EscPosViewDocumentConverter : IViewDocumentConverter
{

    public ViewDocument ToViewDocument(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Protocol != Protocol.EscPos)
        {
            throw new BadRequestException(
                $"View conversion is only supported for EscPos, got {document.Protocol}.");
        }

        var state = RenderState.CreateDefault();
        var elements = new List<ViewElement>();
        var lineBuffer = new LineBufferState();

        foreach (var element in document.Elements)
        {
            switch (element)
            {
                case AppendToLineBuffer textLine:
                    AddDebugElement(elements, textLine, "appendToLineBuffer", new Dictionary<string, string>
                    {
                        ["Text"] = textLine.Text ?? string.Empty
                    });
                    AppendTextSegment(textLine, state, lineBuffer);
                    break;
                case FlushLineBufferAndFeed flushLine:
                    FlushLine(document, state, lineBuffer, elements, includeFlushState: true, flushLine);
                    break;
                case RasterImage raster:
                    FlushLine(document, state, lineBuffer, elements, includeFlushState: false, null);
                    AddDebugElement(elements, raster, "rasterImage", new Dictionary<string, string>());
                    ValidateImageBounds(document, state.CurrentY, raster.Width, raster.Height, elements);
                    AddImageElement(raster, state, elements);
                    break;
                case RasterImageUpload:
                    throw new InvalidOperationException("Upload requests must not be emitted");
                case PrintBarcode barcode:
                    FlushLine(document, state, lineBuffer, elements, includeFlushState: false, null);
                    AddDebugElement(elements, barcode, "printBarcode", new Dictionary<string, string>());
                    ValidateImageBounds(document, state.CurrentY, barcode.Width, barcode.Height, elements);
                    AddImageElement(barcode, state, elements);
                    break;
                case PrintQrCode qrCode:
                    FlushLine(document, state, lineBuffer, elements, includeFlushState: false, null);
                    AddDebugElement(elements, qrCode, "printQrCode", new Dictionary<string, string>());
                    ValidateImageBounds(document, state.CurrentY, qrCode.Width, qrCode.Height, elements);
                    AddImageElement(qrCode, state, elements);
                    break;
                case PrintBarcodeUpload barcodeUpload:
                    AddDebugElement(elements, barcodeUpload, "printBarcode", new Dictionary<string, string>
                    {
                        ["Symbology"] = DomainMapper.ToString(barcodeUpload.Symbology),
                        ["Data"] = barcodeUpload.Data
                    });
                    break;
                case PrintQrCodeUpload qrCodeUpload:
                    AddDebugElement(elements, qrCodeUpload, "printQrCode", new Dictionary<string, string>());
                    break;
                case StoredLogo storedLogo:
                    AddDebugElement(elements, storedLogo, "storedLogo", new Dictionary<string, string>
                    {
                        ["LogoId"] = storedLogo.LogoId.ToString()
                    });
                    break;
                case SetJustification justification:
                    state.Justification = justification.Justification;
                    AddDebugElement(elements, justification, "setJustification", new Dictionary<string, string>
                    {
                        ["Justification"] = DomainMapper.ToString(justification.Justification)
                    });
                    break;
                case SetBoldMode bold:
                    state.IsBold = bold.IsEnabled;
                    AddDebugElement(elements, bold, "setBoldMode", new Dictionary<string, string>
                    {
                        ["IsEnabled"] = bold.IsEnabled.ToString()
                    });
                    break;
                case SetUnderlineMode underline:
                    state.IsUnderline = underline.IsEnabled;
                    AddDebugElement(elements, underline, "setUnderlineMode", new Dictionary<string, string>
                    {
                        ["IsEnabled"] = underline.IsEnabled.ToString()
                    });
                    break;
                case SetReverseMode reverse:
                    state.IsReverse = reverse.IsEnabled;
                    AddDebugElement(elements, reverse, "setReverseMode", new Dictionary<string, string>
                    {
                        ["IsEnabled"] = reverse.IsEnabled.ToString()
                    });
                    break;
                case SetLineSpacing spacing:
                    state.LineSpacing = spacing.Spacing;
                    AddDebugElement(elements, spacing, "setLineSpacing", new Dictionary<string, string>
                    {
                        ["Spacing"] = spacing.Spacing.ToString()
                    });
                    break;
                case ResetLineSpacing resetLineSpacing:
                    state.LineSpacing = EscPosViewConstants.DefaultLineSpacing;
                    AddDebugElement(elements, resetLineSpacing, "resetLineSpacing", new Dictionary<string, string>());
                    break;
                case SetFont font:
                    state.FontNumber = font.FontNumber;
                    state.ScaleX = font.IsDoubleWidth ? 2 : 1;
                    state.ScaleY = font.IsDoubleHeight ? 2 : 1;
                    AddDebugElement(elements, font, "setFont", new Dictionary<string, string>
                    {
                        ["FontNumber"] = font.FontNumber.ToString(),
                        ["IsDoubleWidth"] = font.IsDoubleWidth.ToString(),
                        ["IsDoubleHeight"] = font.IsDoubleHeight.ToString()
                    });
                    break;
                case ResetPrinter resetPrinter:
                    state = RenderState.CreateDefault();
                    AddDebugElement(elements, resetPrinter, "resetPrinter", new Dictionary<string, string>());
                    break;
                default:
                    AddDebugElement(elements, element, GetDebugType(element), BuildStateParameters(element));
                    break;
            }
        }

        FlushLine(document, state, lineBuffer, elements, includeFlushState: false, null);

        // Collect error messages from Error and PrinterError elements
        var errorMessages = document.Elements
            .Where(e => e is Error or PrinterError)
            .Select(e => e switch
            {
                Error error => error.Message ?? error.Code ?? "Unknown error",
                PrinterError printerError => printerError.Message ?? "Printer error",
                _ => null
            })
            .Where(msg => msg is not null)
            .Cast<string>()
            .ToArray();

        return new ViewDocument(
            document.Id,
            document.PrintJobId,
            document.PrinterId,
            document.CreatedAt,
            document.Protocol,
            document.WidthInDots,
            document.HeightInDots,
            document.ClientAddress,
            elements.AsReadOnly(),
            errorMessages is { Length: > 0 } ? errorMessages : null);
    }

    private static void AppendTextSegment(
        AppendToLineBuffer textLine,
        RenderState state,
        LineBufferState lineBuffer)
    {
        var text = textLine.Text ?? string.Empty;
        var fontWidth = GetFontWidth(state.FontNumber) * state.ScaleX;
        var fontHeight = GetFontHeight(state.FontNumber) * state.ScaleY;
        var charSpacing = state.CharSpacing;
        var segmentWidth = CalculateTextWidth(text, fontWidth, charSpacing);
        var startX = lineBuffer.LineWidth;
        lineBuffer.LineWidth += segmentWidth;
        lineBuffer.LineHeight = Math.Max(lineBuffer.LineHeight, fontHeight);

        lineBuffer.Segments.Add(new TextSegment(
            text,
            startX,
            segmentWidth,
            fontHeight,
            GetFontLabel(state.FontNumber),
            charSpacing,
            state.ScaleX,
            state.ScaleY,
            state.IsBold,
            state.IsUnderline,
            state.IsReverse,
            textLine.CommandRaw,
            CommandDescriptionBuilder.Build(textLine),
            textLine.LengthInBytes,
            state.ZIndex));
    }

    private static void FlushLine(
        Document document,
        RenderState state,
        LineBufferState lineBuffer,
        List<ViewElement> elements,
        bool includeFlushState,
        FlushLineBufferAndFeed? flushElement)
    {
        if (lineBuffer.Segments.Count == 0)
        {
            if (includeFlushState && flushElement is not null)
            {
                AddDebugElement(elements, flushElement, "flushLineBufferAndFeed", new Dictionary<string, string>());
            }
            return;
        }

        var baseX = CalculateJustifiedX(document.WidthInDots, lineBuffer.LineWidth, state.Justification);

        if (includeFlushState && flushElement is not null)
        {
            AddDebugElement(elements, flushElement, "flushLineBufferAndFeed", new Dictionary<string, string>());
        }

        foreach (var segment in lineBuffer.Segments)
        {
            var element = new ViewTextElement(
                segment.Text,
                baseX + segment.StartX,
                state.CurrentY,
                segment.Width,
                segment.Height,
                segment.Font,
                segment.CharSpacing,
                segment.IsBold,
                segment.IsUnderline,
                segment.IsReverse)
            {
                CommandRaw = segment.CommandRaw,
                CommandDescription = segment.CommandDescription,
                LengthInBytes = segment.LengthInBytes,
                CharScaleX = segment.ScaleX,
                CharScaleY = segment.ScaleY,
                ZIndex = segment.ZIndex
            };

            elements.Add(element);
        }

        state.CurrentY += lineBuffer.LineHeight + state.LineSpacing;
        lineBuffer.Reset();
    }

    private static void AddImageElement(RasterImage raster, RenderState state, List<ViewElement> elements)
    {
        var media = new ViewMedia(
            raster.Media.ContentType,
            raster.Media.Length,
            raster.Media.Sha256Checksum,
            raster.Media.Url);

        elements.Add(new ViewImageElement(
            media,
            0,
            state.CurrentY,
            raster.Width,
            raster.Height)
        {
            CommandRaw = raster.CommandRaw,
            CommandDescription = CommandDescriptionBuilder.Build(raster),
            LengthInBytes = raster.LengthInBytes,
            ZIndex = state.ZIndex
        });

        state.CurrentY += raster.Height + state.LineSpacing;
    }

    private static void AddImageElement(PrintBarcode barcode, RenderState state, List<ViewElement> elements)
    {
        var media = new ViewMedia(
            barcode.Media.ContentType,
            barcode.Media.Length,
            barcode.Media.Sha256Checksum,
            barcode.Media.Url);

        elements.Add(new ViewImageElement(
            media,
            0,
            state.CurrentY,
            barcode.Width,
            barcode.Height)
        {
            CommandRaw = barcode.CommandRaw,
            CommandDescription = CommandDescriptionBuilder.Build(barcode),
            LengthInBytes = barcode.LengthInBytes,
            ZIndex = state.ZIndex
        });

        state.CurrentY += barcode.Height + state.LineSpacing;
    }

    private static void AddImageElement(PrintQrCode qrCode, RenderState state, List<ViewElement> elements)
    {
        var media = new ViewMedia(
            qrCode.Media.ContentType,
            qrCode.Media.Length,
            qrCode.Media.Sha256Checksum,
            qrCode.Media.Url);

        elements.Add(new ViewImageElement(
            media,
            0,
            state.CurrentY,
            qrCode.Width,
            qrCode.Height)
        {
            CommandRaw = qrCode.CommandRaw,
            CommandDescription = CommandDescriptionBuilder.Build(qrCode),
            LengthInBytes = qrCode.LengthInBytes,
            ZIndex = state.ZIndex
        });

        state.CurrentY += qrCode.Height + state.LineSpacing;
    }

    private static void ValidateImageBounds(
        Document document,
        int currentY,
        int imageWidth,
        int imageHeight,
        List<ViewElement> elements)
    {
        var left = 0;
        var top = currentY;
        var right = left + imageWidth;
        var bottom = top + imageHeight;
        var exceedsWidth = right > document.WidthInDots;
        var exceedsHeight = document.HeightInDots.HasValue && bottom > document.HeightInDots.Value;

        if (!exceedsWidth && !exceedsHeight)
        {
            return;
        }

        // Emit a diagnostic state element before the image when it exceeds printer bounds.
        var message =
            $"Image exceeds printer bounds (left={left}, top={top}, right={right}, bottom={bottom}, " +
            $"printerWidth={document.WidthInDots}, printerHeight={document.HeightInDots?.ToString() ?? "unlimited"}).";
        elements.Add(new ViewDebugElement("error", new Dictionary<string, string>())
        {
            CommandRaw = string.Empty,
            CommandDescription = new[] { message },
            LengthInBytes = 0,
            ZIndex = 0
        });
    }

    private static void AddDebugElement(
        List<ViewElement> elements,
        Element element,
        string debugType,
        IReadOnlyDictionary<string, string> parameters)
    {
        elements.Add(new ViewDebugElement(debugType, parameters)
        {
            CommandRaw = element.CommandRaw,
            CommandDescription = CommandDescriptionBuilder.Build(element),
            LengthInBytes = element.LengthInBytes,
            ZIndex = 0
        });
    }

    private static string GetDebugType(Element element)
    {
        return element switch
        {
            Bell => "bell",
            Error => "error",
            Pagecut => "pagecut",
            PrinterError => "printerError",
            GetPrinterStatus => "printerStatus",
            Pulse => "pulse",
            ResetPrinter => "resetPrinter",
            SetBarcodeHeight => "setBarcodeHeight",
            SetBarcodeLabelPosition => "setBarcodeLabelPosition",
            SetBarcodeModuleWidth => "setBarcodeModuleWidth",
            SetBoldMode => "setBoldMode",
            SetCodePage => "setCodePage",
            SetFont => "setFont",
            SetJustification => "setJustification",
            SetLineSpacing => "setLineSpacing",
            ResetLineSpacing => "resetLineSpacing",
            SetQrErrorCorrection => "setQrErrorCorrection",
            SetQrModel => "setQrModel",
            SetQrModuleSize => "setQrModuleSize",
            SetReverseMode => "setReverseMode",
            SetUnderlineMode => "setUnderlineMode",
            StoreQrData => "storeQrData",
            StatusRequest => "statusRequest",
            StatusResponse => "statusResponse",
            _ => element.GetType().Name
        };
    }

    private static IReadOnlyDictionary<string, string> BuildStateParameters(Element element)
    {
        return element switch
        {
            Error error => new Dictionary<string, string>
            {
                ["Code"] = error.Code,
                ["Message"] = error.Message
            },
            PrinterError printerError => new Dictionary<string, string>
            {
                ["Message"] = printerError.Message
            },
            Pulse pulse => new Dictionary<string, string>
            {
                ["Pin"] = pulse.Pin.ToString(),
                ["OnTimeMs"] = pulse.OnTimeMs.ToString(),
                ["OffTimeMs"] = pulse.OffTimeMs.ToString()
            },
            SetBarcodeHeight height => new Dictionary<string, string>
            {
                ["HeightInDots"] = height.HeightInDots.ToString()
            },
            SetBarcodeLabelPosition position => new Dictionary<string, string>
            {
                ["Position"] = DomainMapper.ToString(position.Position)
            },
            SetBarcodeModuleWidth moduleWidth => new Dictionary<string, string>
            {
                ["ModuleWidth"] = moduleWidth.ModuleWidth.ToString()
            },
            SetBoldMode bold => new Dictionary<string, string>
            {
                ["IsEnabled"] = bold.IsEnabled.ToString()
            },
            SetCodePage codePage => new Dictionary<string, string>
            {
                ["CodePage"] = codePage.CodePage
            },
            SetFont font => new Dictionary<string, string>
            {
                ["FontNumber"] = font.FontNumber.ToString(),
                ["IsDoubleWidth"] = font.IsDoubleWidth.ToString(),
                ["IsDoubleHeight"] = font.IsDoubleHeight.ToString()
            },
            SetJustification justification => new Dictionary<string, string>
            {
                ["Justification"] = DomainMapper.ToString(justification.Justification)
            },
            SetLineSpacing spacing => new Dictionary<string, string>
            {
                ["Spacing"] = spacing.Spacing.ToString()
            },
            ResetLineSpacing => new Dictionary<string, string>(),
            SetQrErrorCorrection correction => new Dictionary<string, string>
            {
                ["Level"] = DomainMapper.ToString(correction.Level)
            },
            SetQrModel model => new Dictionary<string, string>
            {
                ["Model"] = DomainMapper.ToString(model.Model)
            },
            SetQrModuleSize moduleSize => new Dictionary<string, string>
            {
                ["ModuleSize"] = moduleSize.ModuleSize.ToString()
            },
            SetReverseMode reverse => new Dictionary<string, string>
            {
                ["IsEnabled"] = reverse.IsEnabled.ToString()
            },
            SetUnderlineMode underline => new Dictionary<string, string>
            {
                ["IsEnabled"] = underline.IsEnabled.ToString()
            },
            StoreQrData store => new Dictionary<string, string>
            {
                ["Content"] = store.Content
            },
            Pagecut pagecut => new Dictionary<string, string>
            {
                ["Mode"] = pagecut.Mode.ToString(),
                ["FeedMotionUnits"] = pagecut.FeedMotionUnits?.ToString() ?? string.Empty
            },
            StatusRequest request => new Dictionary<string, string>
            {
                ["RequestType"] = request.RequestType.ToString()
            },
            StatusResponse response => new Dictionary<string, string>
            {
                ["StatusByte"] = $"0x{response.StatusByte:X2}",
                ["IsPaperOut"] = response.IsPaperOut.ToString(),
                ["IsCoverOpen"] = response.IsCoverOpen.ToString(),
                ["IsOffline"] = response.IsOffline.ToString()
            },
            _ => new Dictionary<string, string>()
        };
    }

    private static int GetFontWidth(int fontNumber)
    {
        return fontNumber == 1 ? EscPosViewConstants.FontBWidth : EscPosViewConstants.FontAWidth;
    }

    private static int GetFontHeight(int fontNumber)
    {
        return fontNumber == 1 ? EscPosViewConstants.FontBHeight : EscPosViewConstants.FontAHeight;
    }

    private static string? GetFontLabel(int fontNumber)
    {
        return fontNumber == 1 ? ViewFontNames.EscPosB : ViewFontNames.EscPosA;
    }

    private static int CalculateTextWidth(string text, int charWidth, int charSpacing)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var spacing = charSpacing > 0 ? charSpacing : 0;
        return (text.Length * charWidth) + (spacing * Math.Max(0, text.Length - 1));
    }

    private static int CalculateJustifiedX(int totalWidth, int lineWidth, TextJustification justification)
    {
        if (lineWidth <= 0)
        {
            return 0;
        }

        return justification switch
        {
            TextJustification.Center => Math.Max(0, (totalWidth - lineWidth) / 2),
            TextJustification.Right => Math.Max(0, totalWidth - lineWidth),
            _ => 0
        };
    }

    private sealed class RenderState
    {
        public TextJustification Justification { get; set; } = TextJustification.Left;
        public int LineSpacing { get; set; } = EscPosViewConstants.DefaultLineSpacing;
        public int FontNumber { get; set; }
        public int ScaleX { get; set; } = 1;
        public int ScaleY { get; set; } = 1;
        public int CharSpacing { get; set; }
        public bool IsBold { get; set; }
        public bool IsUnderline { get; set; }
        public bool IsReverse { get; set; }
        public int ZIndex { get; set; }
        public int CurrentY { get; set; }

        public static RenderState CreateDefault() => new();
    }

    private sealed class LineBufferState
    {
        public List<TextSegment> Segments { get; } = new();
        public int LineWidth { get; set; }
        public int LineHeight { get; set; }

        public void Reset()
        {
            Segments.Clear();
            LineWidth = 0;
            LineHeight = 0;
        }
    }

    private sealed record TextSegment(
        string Text,
        int StartX,
        int Width,
        int Height,
        string? Font,
        int CharSpacing,
        int ScaleX,
        int ScaleY,
        bool IsBold,
        bool IsUnderline,
        bool IsReverse,
        string CommandRaw,
        IReadOnlyList<string> CommandDescription,
        int LengthInBytes,
        int ZIndex);
}
