using System.Text;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Domain.Documents;
using Printify.Domain.Layout;
using Printify.Domain.Layout.Primitives;
using Printify.Domain.Printing;
using Printify.Domain.Printers;
using Printify.Domain.Specifications;
using Printify.Infrastructure.Printing.EscPos.Commands;
using LayoutMedia = Printify.Domain.Layout.Primitives.Media;
using static Printify.Infrastructure.Printing.EscPos.EscPosCommandHelper;

namespace Printify.Infrastructure.Printing.EscPos.Renderers;

/// <summary>
/// Renders ESC/POS protocol commands to canvases.
/// A new canvas is created on each CutPaper (pagecut) command.
/// </summary>
public sealed class EscPosRenderer : IRenderer
{
    private const int DefaultQrModuleSizeInDots = 4;
    private const int DefaultQrQuietZoneInModules = 4;

    public Canvas[] Render(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Protocol != Protocol.EscPos)
        {
            throw new BadRequestException(
                $"EscPosRenderer only supports EscPos protocol, got {document.Protocol}.");
        }

        var state = RenderState.CreateDefault();
        var canvases = new List<CanvasInfo>();
        var currentItems = new List<BaseElement>();
        var lineBuffer = new LineBufferState();
        var canvasWidthInDots = document.WidthInDots;
        var canvasHeightInDots = document.HeightInDots;

        foreach (var command in document.Commands)
        {
            switch (command)
            {
                case EscPosAppendText textLine:
                    var decodedText = state.CurrentEncoding.GetString(textLine.TextBytes);
                    currentItems.Add(new DebugInfo(
                        "appendToLineBuffer",
                        new Dictionary<string, string>
                        {
                            ["Text"] = decodedText,
                            ["CodePage"] = state.CurrentEncoding.CodePage.ToString()
                        },
                        textLine.RawBytes,
                        textLine.LengthInBytes,
                        GetDescription(textLine, state.CurrentEncoding)));
                    AppendTextSegment(state, lineBuffer, decodedText);
                    break;

                case EscPosPrintAndLineFeed:
                    currentItems.Add(new DebugInfo(
                        "flushLineBufferAndFeed",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    FlushLine(state, lineBuffer, currentItems, canvasWidthInDots);
                    break;

                case EscPosLegacyCarriageReturn:
                    currentItems.Add(new DebugInfo(
                        "legacyCarriageReturn",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case EscPosRasterImage raster:
                    ClearLineBufferWithError(lineBuffer, currentItems, "raster image command");
                    currentItems.Add(new DebugInfo(
                        GetRasterImageElementName(raster),
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    AddImageElement(raster, state, currentItems);
                    break;

                case EscPosRasterImageUploadGs7630:
                case EscPosRasterImageStore:
                case EscPosRasterImagePrintUploadGs284C:
                case EscPosPrintBarcodeUpload:
                case EscPosPrintQrCodeUpload:
                    throw new InvalidOperationException("Upload requests must not be emitted");

                case EscPosPrintBarcode barcode:
                    ClearLineBufferWithError(lineBuffer, currentItems, "barcode command");
                    currentItems.Add(new DebugInfo(
                        "printBarcode",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    AddImageElement(barcode, state, currentItems);
                    break;

                case EscPosPrintQrCode qrCode:
                    ClearLineBufferWithError(lineBuffer, currentItems, "QR code command");
                    currentItems.Add(new DebugInfo(
                        "printQrCode",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    AddImageElement(qrCode, state, currentItems, canvasWidthInDots);
                    break;

                case EscPosSetJustification justification:
                    state.Justification = justification.Justification;
                    currentItems.Add(new DebugInfo(
                        "setJustification",
                        new Dictionary<string, string>
                        {
                            ["Justification"] = justification.Justification.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case EscPosSetBoldMode bold:
                    state.IsBold = bold.IsEnabled;
                    currentItems.Add(new DebugInfo(
                        "setBoldMode",
                        new Dictionary<string, string>
                        {
                            ["IsEnabled"] = bold.IsEnabled.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case EscPosCancelBoldMode:
                    state.IsBold = false;
                    currentItems.Add(new DebugInfo(
                        "cancelBoldMode",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case EscPosSetDoubleStrikeMode doubleStrike:
                    state.IsDoubleStrike = doubleStrike.IsEnabled;
                    currentItems.Add(new DebugInfo(
                        "setDoubleStrikeMode",
                        new Dictionary<string, string>
                        {
                            ["IsEnabled"] = doubleStrike.IsEnabled.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case EscPosEnableItalicMode:
                    state.IsItalic = true;
                    currentItems.Add(new DebugInfo(
                        "enableItalicMode",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case EscPosDisableItalicMode:
                    state.IsItalic = false;
                    currentItems.Add(new DebugInfo(
                        "disableItalicMode",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case EscPosSetUnderlineMode underline:
                    state.IsUnderline = underline.IsEnabled;
                    currentItems.Add(new DebugInfo(
                        "setUnderlineMode",
                        new Dictionary<string, string>
                        {
                            ["IsEnabled"] = underline.IsEnabled.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case EscPosSetReverseMode reverse:
                    state.IsReverse = reverse.IsEnabled;
                    currentItems.Add(new DebugInfo(
                        "setReverseMode",
                        new Dictionary<string, string>
                        {
                            ["IsEnabled"] = reverse.IsEnabled.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case EscPosSetCharacterSize size:
                    state.ScaleX = size.WidthMultiplier;
                    state.ScaleY = size.HeightMultiplier;
                    currentItems.Add(new DebugInfo(
                        "setCharacterSize",
                        new Dictionary<string, string>
                        {
                            ["WidthMultiplier"] = size.WidthMultiplier.ToString(),
                            ["HeightMultiplier"] = size.HeightMultiplier.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case EscPosSetRightCharacterSpacing spacing:
                    state.CharSpacing = spacing.Spacing;
                    currentItems.Add(new DebugInfo(
                        "setRightCharacterSpacing",
                        new Dictionary<string, string>
                        {
                            ["Spacing"] = spacing.Spacing.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case EscPosSetUpsideDownMode upsideDown:
                    state.IsUpsideDown = upsideDown.IsEnabled;
                    currentItems.Add(new DebugInfo(
                        "setUpsideDownMode",
                        new Dictionary<string, string>
                        {
                            ["IsEnabled"] = upsideDown.IsEnabled.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case EscPosSetLineSpacing spacing:
                    state.LineSpacing = spacing.Spacing;
                    currentItems.Add(new DebugInfo(
                        "setLineSpacing",
                        new Dictionary<string, string>
                        {
                            ["Spacing"] = spacing.Spacing.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case EscPosResetLineSpacing:
                    state.LineSpacing = EscPosSpecs.Rendering.DefaultLineSpacing;
                    currentItems.Add(new DebugInfo(
                        "resetLineSpacing",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case EscPosSetCodePage codePage:
                    state.CurrentEncoding = GetEncodingFromCodePage(codePage.CodePage);
                    currentItems.Add(new DebugInfo(
                        "setCodePage",
                        new Dictionary<string, string>
                        {
                            ["CodePage"] = codePage.CodePage
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case EscPosSetPrintMode font:
                    state.FontNumber = font.FontNumber;
                    state.ScaleX = font.IsDoubleWidth ? 2 : 1;
                    state.ScaleY = font.IsDoubleHeight ? 2 : 1;
                    currentItems.Add(new DebugInfo(
                        "setPrintMode",
                        new Dictionary<string, string>
                        {
                            ["FontNumber"] = font.FontNumber.ToString(),
                            ["IsDoubleWidth"] = font.IsDoubleWidth.ToString(),
                            ["IsDoubleHeight"] = font.IsDoubleHeight.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case EscPosPrintAndFeedLines feedLines:
                    currentItems.Add(new DebugInfo(
                        "printAndFeedLines",
                        new Dictionary<string, string>
                        {
                            ["Lines"] = feedLines.Lines.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    FlushLine(state, lineBuffer, currentItems, canvasWidthInDots);
                    var lineAdvance = GetFontHeight(state.FontNumber) * state.ScaleY + state.LineSpacing;
                    state.CurrentY += feedLines.Lines * lineAdvance;
                    break;

                case EscPosSetQrModuleSize qrModuleSize:
                    state.QrModuleSizeInDots = qrModuleSize.ModuleSize;
                    currentItems.Add(new DebugInfo(
                        "setQrModuleSize",
                        new Dictionary<string, string>
                        {
                            ["ModuleSize"] = qrModuleSize.ModuleSize.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case EscPosPrintAndFeedDots feedDots:
                    currentItems.Add(new DebugInfo(
                        "printAndFeedDots",
                        new Dictionary<string, string>
                        {
                            ["Dots"] = feedDots.Dots.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    FlushLine(state, lineBuffer, currentItems, canvasWidthInDots);
                    // ESC J moves paper by a raw dot distance after printing any pending text.
                    state.CurrentY += feedDots.Dots;
                    break;

                case EscPosHorizontalTab:
                    currentItems.Add(new DebugInfo(
                        "horizontalTab",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    ApplyHorizontalTab(state, lineBuffer, currentItems);
                    break;

                case EscPosSetHorizontalTabStops tabStops:
                    state.HorizontalTabStops = tabStops.Columns.ToArray();
                    currentItems.Add(new DebugInfo(
                        "setHorizontalTabStops",
                        new Dictionary<string, string>
                        {
                            ["Columns"] = string.Join(",", tabStops.Columns)
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case EscPosSetFont setFont:
                    state.FontNumber = setFont.FontNumber;
                    currentItems.Add(new DebugInfo(
                        "setFont",
                        new Dictionary<string, string>
                        {
                            ["FontNumber"] = setFont.FontNumber.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case EscPosInitialize:
                    state.Initialize();
                    currentItems.Add(new DebugInfo(
                        "reset",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case EscPosCutPaper:
                    // Flush any unprinted text buffer to surface a printer error for truncated content.
                    ClearLineBufferWithError(lineBuffer, currentItems, "end of page");

                    currentItems.Add(new DebugInfo(
                        "pagecut",
                        BuildStateParameters(command),
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    // Finalize current canvas and start a new one
                    canvases.Add(new CanvasInfo(currentItems.ToList()));
                    currentItems = new List<BaseElement>();
                    // Reset PosX and PosY for new canvas, preserve other state
                    state.CurrentY = 0;
                    break;

                case EscPosPrintLogo logo:
                    ClearLineBufferWithError(lineBuffer, currentItems, "stored logo command");
                    currentItems.Add(new DebugInfo(
                        "storedLogo",
                        new Dictionary<string, string>
                        {
                            ["LogoId"] = logo.LogoId.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                default:
                    currentItems.Add(new DebugInfo(
                        GetDebugType(command),
                        BuildStateParameters(command),
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;
            }
        }

        // Flush any unprinted text buffer to surface a printer error for truncated content.
        ClearLineBufferWithError(lineBuffer, currentItems, "end of document");

        // Add the final canvas if it has items
        if (currentItems.Count > 0 || canvases.Count == 0)
        {
            canvases.Add(new CanvasInfo(currentItems));
        }

        return canvases
            .Select(info => new Canvas(
                WidthInDots: canvasWidthInDots,
                HeightInDots: canvasHeightInDots,
                Items: info.Items))
            .ToArray();
    }

    private static void AppendTextSegment(
        RenderState state,
        LineBufferState lineBuffer,
        string decodedText)
    {
        var text = decodedText;
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
            state.IsItalic,
            state.IsDoubleStrike,
            state.IsUpsideDown,
            state.IsReverse));
    }

    private static void FlushLine(
        RenderState state,
        LineBufferState lineBuffer,
        List<BaseElement> items,
        int canvasWidthInDots)
    {
        if (lineBuffer.Segments.Count == 0)
        {
            return;
        }

        var baseX = CalculateJustifiedX(canvasWidthInDots, lineBuffer.LineWidth, state.Justification);

        foreach (var segment in lineBuffer.Segments)
        {
            var rotation = segment.IsUpsideDown ? Rotation.Rotate180 : Rotation.None;
            items.Add(new TextElement(
                segment.Text,
                baseX + segment.StartX,
                state.CurrentY,
                segment.Width,
                segment.Height,
                segment.Font,
                segment.CharSpacing,
                segment.IsBold,
                segment.IsUnderline,
                segment.IsReverse,
                segment.ScaleX,
                segment.ScaleY,
                rotation,
                segment.IsItalic,
                segment.IsDoubleStrike));
        }

        state.CurrentY += lineBuffer.LineHeight + state.LineSpacing;
        lineBuffer.Reset();
    }

    private static void AddImageElement(EscPosRasterImage escPosRaster, RenderState state, List<BaseElement> items)
    {
        items.Add(new ImageElement(
            new LayoutMedia(
                escPosRaster.Media.ContentType,
                ToMediaSize(escPosRaster.Media.Length),
                escPosRaster.Media.Url,
                escPosRaster.Media.Sha256Checksum),
            0,
            state.CurrentY,
            escPosRaster.Width,
            escPosRaster.Height,
            Rotation.None));

        state.CurrentY += escPosRaster.Height + state.LineSpacing;
    }

    private static string GetRasterImageElementName(EscPosRasterImage raster)
    {
        return raster switch
        {
            EscPosRasterImageGs7630 => "rasterImageGs7630",
            EscPosRasterImageGs284C => "rasterImageGs284C",
            EscPosRasterImageGs384C => "rasterImageGs384C",
            _ => "rasterImage"
        };
    }

    private static void AddImageElement(EscPosPrintBarcode barcode, RenderState state, List<BaseElement> items)
    {
        items.Add(new ImageElement(
            new LayoutMedia(
                barcode.Media.ContentType,
                ToMediaSize(barcode.Media.Length),
                barcode.Media.Url,
                barcode.Media.Sha256Checksum),
            0,
            state.CurrentY,
            barcode.Width,
            barcode.Height,
            Rotation.None));

        state.CurrentY += barcode.Height + state.LineSpacing;
    }

    private static void AddImageElement(
        EscPosPrintQrCode qrCode,
        RenderState state,
        List<BaseElement> items,
        int canvasWidthInDots)
    {
        var quietZoneInDots = CalculateQrQuietZoneInDots(state.QrModuleSizeInDots);
        var x = CalculateJustifiedX(canvasWidthInDots, qrCode.Width, state.Justification);

        items.Add(new ImageElement(
            new LayoutMedia(
                qrCode.Media.ContentType,
                ToMediaSize(qrCode.Media.Length),
                qrCode.Media.Url,
                qrCode.Media.Sha256Checksum),
            x,
            state.CurrentY + quietZoneInDots,
            qrCode.Width,
            qrCode.Height,
            Rotation.None));

        state.CurrentY += qrCode.Height + quietZoneInDots * 2 + state.LineSpacing;
    }

    private static void ApplyHorizontalTab(RenderState state, LineBufferState lineBuffer, List<BaseElement> items)
    {
        var currentAdvance = GetFontWidth(state.FontNumber) * state.ScaleX + Math.Max(0, state.CharSpacing);
        if (currentAdvance <= 0)
        {
            return;
        }

        var currentColumn = lineBuffer.LineWidth / currentAdvance;
        var nextTabColumn = GetNextHorizontalTabColumn(state.HorizontalTabStops, currentColumn);
        if (!nextTabColumn.HasValue)
        {
            return;
        }

        var nextX = nextTabColumn.Value * currentAdvance;
        if (nextX > lineBuffer.LineWidth)
        {
            // A tab only affects the cursor position inside the pending line buffer.
            lineBuffer.LineWidth = nextX;
        }
    }

    private static int? GetNextHorizontalTabColumn(IReadOnlyList<int>? configuredStops, int currentColumn)
    {
        if (configuredStops is not null)
        {
            foreach (var stop in configuredStops)
            {
                if (stop > currentColumn)
                {
                    return stop;
                }
            }

            return null;
        }

        // ESC/POS default tab interval is every 8 character columns.
        return ((currentColumn / 8) + 1) * 8;
    }

    private static string GetDebugType(Command command)
    {
        return command switch
        {
            EscPosBell => "bell",
            EscPosParseError => "error",
            EscPosCutPaper => "pagecut",
            EscPosPrinterError => "printerError",
            EscPosGetPrinterStatus => "printerStatus",
            EscPosPulse => "pulse",
            EscPosInitialize => "reset",
            EscPosSetBarcodeHeight => "setBarcodeHeight",
            EscPosSetBarcodeLabelPosition => "setBarcodeLabelPosition",
            EscPosSetBarcodeModuleWidth => "setBarcodeModuleWidth",
            EscPosSetBoldMode => "setBoldMode",
            EscPosCancelBoldMode => "cancelBoldMode",
            EscPosSetDoubleStrikeMode => "setDoubleStrikeMode",
            EscPosSetCodePage => "setCodePage",
            EscPosSetPrintMode => "setFont",
            EscPosSetCharacterSize => "setCharacterSize",
            EscPosSetRightCharacterSpacing => "setRightCharacterSpacing",
            EscPosSetFont => "setFont",
            EscPosPrintAndFeedLines => "printAndFeedLines",
            EscPosPrintAndFeedDots => "printAndFeedDots",
            EscPosHorizontalTab => "horizontalTab",
            EscPosSetHorizontalTabStops => "setHorizontalTabStops",
            EscPosSetJustification => "setJustification",
            EscPosSetLineSpacing => "setLineSpacing",
            EscPosResetLineSpacing => "resetLineSpacing",
            EscPosSetQrErrorCorrection => "setQrErrorCorrection",
            EscPosSetQrModel => "setQrModel",
            EscPosSetQrModuleSize => "setQrModuleSize",
            EscPosSetReverseMode => "setReverseMode",
            EscPosEnableItalicMode => "enableItalicMode",
            EscPosDisableItalicMode => "disableItalicMode",
            EscPosSetUpsideDownMode => "setUpsideDownMode",
            EscPosSetUnderlineMode => "setUnderlineMode",
            EscPosStoreQrData => "storeQrData",
            EscPosStatusRequest => "statusRequest",
            EscPosStatusResponse => "statusResponse",
            EscPosLegacyCarriageReturn => "legacyCarriageReturn",
            EscPosPrintLogo => "storedLogo",
            _ => command.GetType().Name
        };
    }

    private static Dictionary<string, string> BuildStateParameters(Command command)
    {
        return command switch
        {
            EscPosParseError error => new Dictionary<string, string>
            {
                ["Code"] = error.Code ?? string.Empty,
                ["Message"] = error.Message ?? "Unknown error"
            },
            EscPosPrinterError printerError => new Dictionary<string, string>
            {
                ["Message"] = printerError.Message ?? "Printer error"
            },
            EscPosPulse pulse => new Dictionary<string, string>
            {
                ["Pin"] = pulse.Pin.ToString(),
                ["OnTimeMs"] = pulse.OnTimeMs.ToString(),
                ["OffTimeMs"] = pulse.OffTimeMs.ToString()
            },
            EscPosSetBarcodeHeight height => new Dictionary<string, string>
            {
                ["HeightInDots"] = height.HeightInDots.ToString()
            },
            EscPosCutPaper pagecut => new Dictionary<string, string>
            {
                ["Mode"] = pagecut.Mode.ToString(),
                ["FeedMotionUnits"] = pagecut.FeedMotionUnits?.ToString() ?? string.Empty
            },
            EscPosPrintAndFeedDots feedDots => new Dictionary<string, string>
            {
                ["Dots"] = feedDots.Dots.ToString()
            },
            EscPosSetHorizontalTabStops tabStops => new Dictionary<string, string>
            {
                ["Columns"] = string.Join(",", tabStops.Columns)
            },
            _ => new Dictionary<string, string>()
        };
    }

    private static int GetFontWidth(int fontNumber) =>
        EscPosSpecs.Fonts.GetWidth(fontNumber);

    private static int GetFontHeight(int fontNumber) =>
        EscPosSpecs.Fonts.GetHeight(fontNumber);

    private static string GetFontLabel(int fontNumber) =>
        EscPosSpecs.Fonts.GetName(fontNumber);

    private static int CalculateTextWidth(string text, int charWidth, int charSpacing)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var spacing = charSpacing > 0 ? charSpacing : 0;
        return (text.Length * charWidth) + (spacing * Math.Max(0, text.Length - 1));
    }

    private static int CalculateJustifiedX(int totalWidth, int lineWidth, EscPosTextJustification justification)
    {
        if (lineWidth <= 0)
        {
            return 0;
        }

        return justification switch
        {
            EscPosTextJustification.Center => Math.Max(0, (totalWidth - lineWidth) / 2),
            EscPosTextJustification.Right => Math.Max(0, totalWidth - lineWidth),
            _ => 0
        };
    }

    private static int CalculateQrQuietZoneInDots(int moduleSizeInDots)
    {
        return Math.Max(2, moduleSizeInDots) * DefaultQrQuietZoneInModules;
    }

    private static Encoding GetEncodingFromCodePage(string codePage)
    {
        try
        {
            return int.TryParse(codePage, out var codePageInt)
                ? Encoding.GetEncoding(codePageInt)
                : Encoding.GetEncoding(codePage);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return Encoding.GetEncoding(437);
        }
    }

    private sealed class RenderState
    {
        public EscPosTextJustification Justification { get; set; } = EscPosTextJustification.Left;
        public int CharSpacing { get; set; } = EscPosSpecs.Rendering.DefaultCharSpacing;
        public int LineSpacing { get; set; } = EscPosSpecs.Rendering.DefaultLineSpacing;
        public int FontNumber { get; set; }
        public int ScaleX { get; set; } = 1;
        public int ScaleY { get; set; } = 1;
        public bool IsBold { get; set; }
        public bool IsDoubleStrike { get; set; }
        public bool IsUnderline { get; set; }
        public bool IsItalic { get; set; }
        public bool IsUpsideDown { get; set; }
        public bool IsReverse { get; set; }
        public int CurrentY { get; set; }
        public int QrModuleSizeInDots { get; set; } = DefaultQrModuleSizeInDots;
        public int[]? HorizontalTabStops { get; set; }
        public Encoding CurrentEncoding { get; set; } = Encoding.GetEncoding(437);

        public static RenderState CreateDefault() => new();

        public void Initialize()
        {
            Justification = EscPosTextJustification.Left;
            CharSpacing = EscPosSpecs.Rendering.DefaultCharSpacing;
            LineSpacing = EscPosSpecs.Rendering.DefaultLineSpacing;
            FontNumber = 0;
            ScaleX = 1;
            ScaleY = 1;
            IsBold = false;
            IsDoubleStrike = false;
            IsUnderline = false;
            IsItalic = false;
            IsUpsideDown = false;
            IsReverse = false;
            HorizontalTabStops = null;
            QrModuleSizeInDots = DefaultQrModuleSizeInDots;
            CurrentEncoding = Encoding.GetEncoding(437);
            // ESC @ does not reset the print position (CurrentY), only printer settings
        }
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

        public (string content, int byteCount) GetContent()
        {
            if (Segments.Count == 0)
            {
                return (string.Empty, 0);
            }

            var content = string.Concat(Segments.Select(s => s.Text));
            // Byte count is sum of all segment lengths
            var byteCount = Segments.Sum(s => s.Text.Length); // For ASCII, 1 char = 1 byte
            return (content, byteCount);
        }
    }

    private static void ClearLineBufferWithError(
        LineBufferState lineBuffer,
        List<BaseElement> items,
        string commandName)
    {
        var (content, byteCount) = lineBuffer.GetContent();
        if (string.IsNullOrEmpty(content))
        {
            lineBuffer.Reset();
            return;
        }

        // Add printer error for lost buffer content
        var description = new List<string>
        {
            $"Text buffer cleared by {commandName}",
            $"{byteCount} bytes lost (\"{content}\")"
        };

        items.Add(new DebugInfo(
            "printerError",
            new Dictionary<string, string>
            {
                ["Message"] = $"Text buffer cleared by {commandName}, {byteCount} bytes lost (\"{content}\")"
            },
            [],
            0,
            description));

        lineBuffer.Reset();
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
        bool IsItalic,
        bool IsDoubleStrike,
        bool IsUpsideDown,
        bool IsReverse);

    private sealed record CanvasInfo(IReadOnlyList<BaseElement> Items);

    private static int ToMediaSize(long length)
    {
        // Clamp to int to satisfy layout metadata without overflowing.
        return length > int.MaxValue ? int.MaxValue : (int)length;
    }
}
