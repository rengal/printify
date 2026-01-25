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

namespace Printify.Infrastructure.Printing.EscPos.Renderers;

/// <summary>
/// Renders ESC/POS protocol commands to a canvas.
/// </summary>
public sealed class EscPosRenderer : IRenderer
{
    public Canvas Render(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Protocol != Protocol.EscPos)
        {
            throw new BadRequestException(
                $"EscPosRenderer only supports EscPos protocol, got {document.Protocol}.");
        }

        var state = RenderState.CreateDefault();
        var items = new List<BaseElement>();
        var lineBuffer = new LineBufferState();
        var canvasWidthInDots = document.WidthInDots;
        var canvasHeightInDots = document.HeightInDots;

        foreach (var command in document.Commands)
        {
            switch (command)
            {
                case AppendText textLine:
                    var decodedText = state.CurrentEncoding.GetString(textLine.TextBytes);
                    items.Add(new DebugInfo(
                        "appendToLineBuffer",
                        new Dictionary<string, string>
                        {
                            ["Text"] = decodedText,
                            ["CodePage"] = state.CurrentEncoding.CodePage.ToString()
                        },
                        textLine.RawBytes,
                        textLine.LengthInBytes,
                        CommandDescriptionBuilder.Build(textLine)));
                    AppendTextSegment(state, lineBuffer, decodedText);
                    break;

                case PrintAndLineFeed:
                    items.Add(new DebugInfo(
                        "flushLineBufferAndFeed",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    FlushLine(state, lineBuffer, items, canvasWidthInDots);
                    break;

                case LegacyCarriageReturn:
                    items.Add(new DebugInfo(
                        "legacyCarriageReturn",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    break;

                case RasterImage raster:
                    ClearLineBufferWithError(lineBuffer, items, "raster image command");
                    items.Add(new DebugInfo(
                        "rasterImage",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    AddImageElement(raster, state, items);
                    break;

                case RasterImageUpload:
                    throw new InvalidOperationException("Upload requests must not be emitted");

                case PrintBarcode barcode:
                    ClearLineBufferWithError(lineBuffer, items, "barcode command");
                    items.Add(new DebugInfo(
                        "printBarcode",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    AddImageElement(barcode, state, items);
                    break;

                case PrintQrCode qrCode:
                    ClearLineBufferWithError(lineBuffer, items, "QR code command");
                    items.Add(new DebugInfo(
                        "printQrCode",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    AddImageElement(qrCode, state, items);
                    break;

                case SetJustification justification:
                    state.Justification = justification.Justification;
                    items.Add(new DebugInfo(
                        "setJustification",
                        new Dictionary<string, string>
                        {
                            ["Justification"] = justification.Justification.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    break;

                case SetBoldMode bold:
                    state.IsBold = bold.IsEnabled;
                    items.Add(new DebugInfo(
                        "setBoldMode",
                        new Dictionary<string, string>
                        {
                            ["IsEnabled"] = bold.IsEnabled.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    break;

                case SetUnderlineMode underline:
                    state.IsUnderline = underline.IsEnabled;
                    items.Add(new DebugInfo(
                        "setUnderlineMode",
                        new Dictionary<string, string>
                        {
                            ["IsEnabled"] = underline.IsEnabled.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    break;

                case SetReverseMode reverse:
                    state.IsReverse = reverse.IsEnabled;
                    items.Add(new DebugInfo(
                        "setReverseMode",
                        new Dictionary<string, string>
                        {
                            ["IsEnabled"] = reverse.IsEnabled.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    break;

                case SetLineSpacing spacing:
                    state.LineSpacing = spacing.Spacing;
                    items.Add(new DebugInfo(
                        "setLineSpacing",
                        new Dictionary<string, string>
                        {
                            ["Spacing"] = spacing.Spacing.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    break;

                case ResetLineSpacing:
                    state.LineSpacing = EscPosSpecs.Rendering.DefaultLineSpacing;
                    items.Add(new DebugInfo(
                        "resetLineSpacing",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    break;

                case SetCodePage codePage:
                    state.CurrentEncoding = GetEncodingFromCodePage(codePage.CodePage);
                    items.Add(new DebugInfo(
                        "setCodePage",
                        new Dictionary<string, string>
                        {
                            ["CodePage"] = codePage.CodePage
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    break;

                case SelectFont font:
                    state.FontNumber = font.FontNumber;
                    state.ScaleX = font.IsDoubleWidth ? 2 : 1;
                    state.ScaleY = font.IsDoubleHeight ? 2 : 1;
                    items.Add(new DebugInfo(
                        "setFont",
                        new Dictionary<string, string>
                        {
                            ["FontNumber"] = font.FontNumber.ToString(),
                            ["IsDoubleWidth"] = font.IsDoubleWidth.ToString(),
                            ["IsDoubleHeight"] = font.IsDoubleHeight.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    break;

                case Initialize:
                    state = RenderState.CreateDefault();
                    items.Add(new DebugInfo(
                        "resetPrinter",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    break;

                case CutPaper:
                    items.Add(new DebugInfo(
                        "pagecut",
                        BuildStateParameters(command),
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    break;

                case StoredLogo logo:
                    ClearLineBufferWithError(lineBuffer, items, "stored logo command");
                    items.Add(new DebugInfo(
                        "storedLogo",
                        new Dictionary<string, string>
                        {
                            ["LogoId"] = logo.LogoId.ToString()
                        },
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    break;

                default:
                    items.Add(new DebugInfo(
                        GetDebugType(command),
                        BuildStateParameters(command),
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    break;
            }
        }

        // Flush any unprinted text buffer to surface a printer error for truncated content.
        ClearLineBufferWithError(lineBuffer, items, "end of document");

        return new Canvas(
            WidthInDots: canvasWidthInDots,
            HeightInDots: canvasHeightInDots,
            Items: items.AsReadOnly());
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
                Rotation.None));
        }

        state.CurrentY += lineBuffer.LineHeight + state.LineSpacing;
        lineBuffer.Reset();
    }

    private static void AddImageElement(RasterImage raster, RenderState state, List<BaseElement> items)
    {
        items.Add(new ImageElement(
            new LayoutMedia(
                raster.Media.ContentType,
                ToMediaSize(raster.Media.Length),
                raster.Media.Url,
                raster.Media.Sha256Checksum),
            0,
            state.CurrentY,
            raster.Width,
            raster.Height,
            Rotation.None));

        state.CurrentY += raster.Height + state.LineSpacing;
    }

    private static void AddImageElement(PrintBarcode barcode, RenderState state, List<BaseElement> items)
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

    private static void AddImageElement(PrintQrCode qrCode, RenderState state, List<BaseElement> items)
    {
        items.Add(new ImageElement(
            new LayoutMedia(
                qrCode.Media.ContentType,
                ToMediaSize(qrCode.Media.Length),
                qrCode.Media.Url,
                qrCode.Media.Sha256Checksum),
            0,
            state.CurrentY,
            qrCode.Width,
            qrCode.Height,
            Rotation.None));

        state.CurrentY += qrCode.Height + state.LineSpacing;
    }

    private static string GetDebugType(Command command)
    {
        return command switch
        {
            Bell => "bell",
            ParseError => "error",
            CutPaper => "pagecut",
            PrinterError => "printerError",
            GetPrinterStatus => "printerStatus",
            Pulse => "pulse",
            Initialize => "resetPrinter",
            SetBarcodeHeight => "setBarcodeHeight",
            SetBarcodeLabelPosition => "setBarcodeLabelPosition",
            SetBarcodeModuleWidth => "setBarcodeModuleWidth",
            SetBoldMode => "setBoldMode",
            SetCodePage => "setCodePage",
            SelectFont => "setFont",
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
            LegacyCarriageReturn => "legacyCarriageReturn",
            StoredLogo => "storedLogo",
            _ => command.GetType().Name
        };
    }

    private static Dictionary<string, string> BuildStateParameters(Command command)
    {
        return command switch
        {
            ParseError error => new Dictionary<string, string>
            {
                ["Code"] = error.Code ?? string.Empty,
                ["Message"] = error.Message ?? "Unknown error"
            },
            PrinterError printerError => new Dictionary<string, string>
            {
                ["Message"] = printerError.Message ?? "Printer error"
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
            CutPaper pagecut => new Dictionary<string, string>
            {
                ["Mode"] = pagecut.Mode.ToString(),
                ["FeedMotionUnits"] = pagecut.FeedMotionUnits?.ToString() ?? string.Empty
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
        public TextJustification Justification { get; set; } = TextJustification.Left;
        public int LineSpacing { get; set; } = EscPosSpecs.Rendering.DefaultLineSpacing;
        public int FontNumber { get; set; }
        public int ScaleX { get; set; } = 1;
        public int ScaleY { get; set; } = 1;
        public int CharSpacing => 0;
        public bool IsBold { get; set; }
        public bool IsUnderline { get; set; }
        public bool IsReverse { get; set; }
        public int CurrentY { get; set; }
        public Encoding CurrentEncoding { get; set; } = Encoding.GetEncoding(437);

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
        bool IsReverse);

    private static int ToMediaSize(long length)
    {
        // Clamp to int to satisfy layout metadata without overflowing.
        return length > int.MaxValue ? int.MaxValue : (int)length;
    }
}
