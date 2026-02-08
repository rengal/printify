using System.Text;
using Printify.Application.Exceptions;
using Printify.Application.Interfaces;
using Printify.Domain.Documents;
using Printify.Domain.Layout;
using Printify.Domain.Layout.Primitives;
using Printify.Domain.Printers;
using Printify.Domain.Printing;
using Printify.Domain.Specifications;
using Printify.Infrastructure.Printing.Epl.Commands;
using LayoutMedia = Printify.Domain.Layout.Primitives.Media;
using EplRotationMapper = Printify.Infrastructure.Mapping.Protocols.Epl.RotationMapper;
using static Printify.Infrastructure.Printing.Epl.EplCommandHelper;

namespace Printify.Infrastructure.Printing.Epl.Renderers;

/// <summary>
/// Renders EPL protocol commands to canvases.
/// A new canvas is generated on each Print (P) command.
/// The first canvas contains all debug elements, subsequent canvases only contain visual elements.
/// </summary>
public sealed class EplRenderer : IRenderer
{
    public Canvas[] Render(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Protocol != Protocol.Epl)
        {
            throw new BadRequestException(
                $"EplRenderer only supports Epl protocol, got {document.Protocol}.");
        }

        var state = RenderState.CreateDefault();
        var canvases = new List<Canvas>();
        var debugElements = new List<BaseElement>();
        var viewElements = new List<BaseElement>();

        // Process all commands - debug elements go to debugElements, visual elements go to viewElements
        foreach (var command in document.Commands)
        {
            switch (command)
            {
                case EplRasterImageUpload or EplPrintBarcodeUpload:
                    // Upload requests must not be emitted (should be finalized before rendering)
                    throw new InvalidOperationException("Upload requests must not be emitted");

                case EplRasterImage rasterImage:
                    AddRasterImageDebugElement(rasterImage, debugElements);
                    AddRasterImageVisualElement(rasterImage, viewElements);
                    break;

                case EplPrintBarcode barcode:
                    AddBarcodeDebugElement(barcode, debugElements);
                    AddBarcodeVisualElement(barcode, viewElements);
                    break;

                case ScalableText scalableText:
                    AddScalableTextDebugElement(scalableText, state, debugElements);
                    AddScalableTextVisualElement(scalableText, state, viewElements);
                    break;

                case DrawHorizontalLine horizontalLine:
                    AddDrawHorizontalLineDebugElement(horizontalLine, debugElements);
                    AddDrawHorizontalLineVisualElement(horizontalLine, viewElements);
                    break;

                case Print print:
                    debugElements.Add(new DebugInfo(
                        "print",
                        new Dictionary<string, string>
                        {
                            ["Copies"] = print.Copies.ToString()
                        },
                        print.RawBytes,
                        print.LengthInBytes,
                        GetDescription(print)));
                    // On Print command, produce canvases with debug + view elements
                    ProduceCanvases(document, debugElements, viewElements, print.Copies, canvases);
                    // Clear elements for next canvas
                    debugElements.Clear();
                    viewElements.Clear();
                    break;

                // Legacy PrintBarcode without media (for backward compatibility)
                case PrintBarcode legacyBarcode:
                    AddLegacyBarcodeDebugElement(legacyBarcode, debugElements);
                    AddLegacyBarcodeVisualElement(legacyBarcode, viewElements);
                    break;

                case DrawBox drawLine:
                    AddDrawLineDebugElement(drawLine, debugElements);
                    AddDrawLineVisualElement(drawLine, viewElements);
                    break;

                case ClearBuffer:
                    debugElements.Add(new DebugInfo(
                        "clearBuffer",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case CarriageReturn:
                    debugElements.Add(new DebugInfo(
                        "carriageReturn",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case LineFeed:
                    debugElements.Add(new DebugInfo(
                        "lineFeed",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;

                case SetLabelWidth labelWidth:
                    debugElements.Add(new DebugInfo(
                        "setLabelWidth",
                        new Dictionary<string, string>
                        {
                            ["Width"] = labelWidth.Width.ToString()
                        },
                        labelWidth.RawBytes,
                        labelWidth.LengthInBytes,
                        GetDescription(labelWidth)));
                    break;

                case SetLabelHeight labelHeight:
                    debugElements.Add(new DebugInfo(
                        "setLabelHeight",
                        new Dictionary<string, string>
                        {
                            ["Height"] = labelHeight.Height.ToString(),
                            ["SecondParameter"] = labelHeight.SecondParameter.ToString()
                        },
                        labelHeight.RawBytes,
                        labelHeight.LengthInBytes,
                        GetDescription(labelHeight)));
                    break;

                case SetPrintSpeed speed:
                    debugElements.Add(new DebugInfo(
                        "setPrintSpeed",
                        new Dictionary<string, string>
                        {
                            ["Speed"] = speed.Speed.ToString()
                        },
                        speed.RawBytes,
                        speed.LengthInBytes,
                        GetDescription(speed)));
                    break;

                case SetPrintDarkness darkness:
                    debugElements.Add(new DebugInfo(
                        "setPrintDarkness",
                        new Dictionary<string, string>
                        {
                            ["Darkness"] = darkness.Darkness.ToString()
                        },
                        darkness.RawBytes,
                        darkness.LengthInBytes,
                        GetDescription(darkness)));
                    break;

                case SetPrintDirection direction:
                    debugElements.Add(new DebugInfo(
                        "setPrintDirection",
                        new Dictionary<string, string>
                        {
                            ["Direction"] = direction.Direction.ToString()
                        },
                        direction.RawBytes,
                        direction.LengthInBytes,
                        GetDescription(direction)));
                    break;

                case SetInternationalCharacter intlChar:
                    state.CurrentEncoding = GetEncodingFromCodePage(intlChar.P1, intlChar.P2, intlChar.P3);
                    debugElements.Add(new DebugInfo(
                        "setInternationalCharacter",
                        new Dictionary<string, string>
                        {
                            ["P1"] = intlChar.P1.ToString(),
                            ["P2"] = intlChar.P2.ToString(),
                            ["P3"] = intlChar.P3.ToString()
                        },
                        intlChar.RawBytes,
                        intlChar.LengthInBytes,
                        GetDescription(intlChar)));
                    break;

                case ParseError error:
                    debugElements.Add(new DebugInfo(
                        "error",
                        new Dictionary<string, string>
                        {
                            ["Code"] = error.Code ?? string.Empty,
                            ["Message"] = error.Message ?? "Unknown error"
                        },
                        error.RawBytes,
                        error.LengthInBytes,
                        GetDescription(error)));
                    break;

                case PrinterError printerError:
                    debugElements.Add(new DebugInfo(
                        "printerError",
                        new Dictionary<string, string>
                        {
                            ["Message"] = printerError.Message ?? "Printer error"
                        },
                        printerError.RawBytes,
                        printerError.LengthInBytes,
                        GetDescription(printerError)));
                    break;

                default:
                    debugElements.Add(new DebugInfo(
                        command.GetType().Name,
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        GetDescription(command)));
                    break;
            }
        }

        // After processing all commands, check if there are any remaining elements
        if (debugElements.Count > 0 || viewElements.Count > 0)
        {
            // If there are view elements without a Print command, add buffer discarded warning
            if (viewElements.Count > 0)
            {
                debugElements.Add(new DebugInfo(
                    "bufferDiscarded",
                    new Dictionary<string, string>
                    {
                        ["Message"] = $"{viewElements} commands in buffer discarded"
                    },
                    [],
                    0,
                    new List<string>
                    {
                        $"{viewElements} commands in buffer discarded (no Print command)"
                    }));
            }

            // Return single canvas with debug elements only (no visual elements)
            var canvas = new Canvas(
                WidthInDots: document.WidthInDots,
                HeightInDots: document.HeightInDots,
                Items: debugElements.AsReadOnly());
            canvases.Add(canvas);
        }

        // Return canvases produced by Print commands (or empty array if none)
        return canvases.ToArray();
    }

    /// <summary>
    /// Produces canvases with debug elements first, then visual elements.
    /// Creates multiple copies if copies > 1.
    /// </summary>
    private static void ProduceCanvases(
        Document document,
        List<BaseElement> debugElements,
        List<BaseElement> viewElements,
        int copies,
        List<Canvas> canvases)
    {
        var allElements = debugElements.Concat(viewElements).ToList();

        // Don't produce empty canvases
        if (allElements.Count == 0 && viewElements.Count == 0)
        {
            return;
        }

        for (int copy = 0; copy < copies; copy++)
        {
            if (copy == 0)
            {
                // First copy: all elements (debug + visual)
                canvases.Add(new Canvas(
                    WidthInDots: document.WidthInDots,
                    HeightInDots: document.HeightInDots,
                    Items: allElements.AsReadOnly()));
            }
            else
            {
                // Subsequent copies: only visual elements (no debug)
                // Only create if there are visual elements
                if (viewElements.Count > 0)
                {
                    var visualOnlyElements = viewElements.Select(CloneVisualElement).ToList();
                    canvases.Add(new Canvas(
                        WidthInDots: document.WidthInDots,
                        HeightInDots: document.HeightInDots,
                        Items: visualOnlyElements.AsReadOnly()));
                }
            }
        }
    }

    private static BaseElement CloneVisualElement(BaseElement element)
    {
        return element switch
        {
            TextElement text => new TextElement(
                text.Text,
                text.X,
                text.Y,
                text.Width,
                text.Height,
                text.FontName,
                text.CharSpacing,
                text.IsBold,
                text.IsUnderline,
                text.IsReverse,
                text.CharScaleX,
                text.CharScaleY,
                text.Rotation),
            ImageElement image => new ImageElement(
                image.Media,
                image.X,
                image.Y,
                image.Width,
                image.Height,
                image.Rotation),
            LineElement line => new LineElement(
                line.X1,
                line.Y1,
                line.X2,
                line.Y2,
                line.Thickness),
            BoxElement box => new BoxElement(
                box.X,
                box.Y,
                box.Width,
                box.Height,
                box.Thickness,
                box.IsFilled),
            _ => element
        };
    }

    private static void AddScalableTextDebugElement(ScalableText scalableText, RenderState state, List<BaseElement> debugElements)
    {
        // Decode raw bytes using current codepage
        var decodedText = state.CurrentEncoding.GetString(scalableText.TextBytes);

        // Add debug element for the command
        debugElements.Add(new DebugInfo(
            "scalableText",
            new Dictionary<string, string>
            {
                ["X"] = scalableText.X.ToString(),
                ["Y"] = scalableText.Y.ToString(),
                ["Rotation"] = scalableText.Rotation.ToString(),
                ["Font"] = scalableText.Font.ToString(),
                ["HorizontalMultiplication"] = scalableText.HorizontalMultiplication.ToString(),
                ["VerticalMultiplication"] = scalableText.VerticalMultiplication.ToString(),
                ["Reverse"] = scalableText.Reverse.ToString(),
                ["Text"] = decodedText
            },
            scalableText.RawBytes,
            scalableText.LengthInBytes,
            GetDescription(scalableText)));
    }

    private static void AddScalableTextVisualElement(ScalableText scalableText, RenderState state, List<BaseElement> viewElements)
    {
        // Decode raw bytes using current codepage
        var decodedText = state.CurrentEncoding.GetString(scalableText.TextBytes);

        // Get font base dimensions
        var (baseWidth, baseHeight) = GetFontDimensions(scalableText.Font);
        var width = baseWidth * scalableText.HorizontalMultiplication * decodedText.Length;
        var height = baseHeight * scalableText.VerticalMultiplication;

        // Calculate actual rendered dimensions based on rotation
        var (renderedWidth, renderedHeight) = CalculateRotatedDimensions(
            width,
            height,
            scalableText.Rotation);

        // Add the text element
        viewElements.Add(new TextElement(
            decodedText,
            scalableText.X,
            scalableText.Y,
            renderedWidth,
            renderedHeight,
            GetFontName(scalableText.Font),
            0, // CharSpacing not applicable for EPL scalable text
            false, // IsBold - EPL doesn't have bold
            false, // IsUnderline
            scalableText.Reverse == 'R', // IsReverse
            scalableText.HorizontalMultiplication,
            scalableText.VerticalMultiplication,
            EplRotationMapper.ToDomainRotation(scalableText.Rotation)));
    }

    private static void AddDrawHorizontalLineDebugElement(DrawHorizontalLine horizontalLine, List<BaseElement> debugElements)
    {
        debugElements.Add(new DebugInfo(
            "drawHorizontalLine",
            new Dictionary<string, string>
            {
                ["X"] = horizontalLine.X.ToString(),
                ["Y"] = horizontalLine.Y.ToString(),
                ["Thickness"] = horizontalLine.Thickness.ToString(),
                ["Length"] = horizontalLine.Length.ToString()
            },
            horizontalLine.RawBytes,
            horizontalLine.LengthInBytes,
            GetDescription(horizontalLine)));
    }

    private static void AddDrawHorizontalLineVisualElement(DrawHorizontalLine horizontalLine, List<BaseElement> viewElements)
    {
        // Horizontal lines are represented as text elements for rendering
        viewElements.Add(new TextElement(
            string.Empty, // No actual text content
            horizontalLine.X,
            horizontalLine.Y,
            horizontalLine.Length,
            horizontalLine.Thickness,
            null, // No font
            0,
            false,
            false,
            false,
            1,
            1,
            0));
    }

    private static void AddBarcodeDebugElement(PrintBarcode barcode, List<BaseElement> debugElements)
    {
        debugElements.Add(new DebugInfo(
            "printBarcode",
            new Dictionary<string, string>
            {
                ["X"] = barcode.X.ToString(),
                ["Y"] = barcode.Y.ToString(),
                ["Rotation"] = barcode.Rotation.ToString(),
                ["Type"] = barcode.Type,
                ["Width"] = barcode.Width.ToString(),
                ["Height"] = barcode.Height.ToString(),
                ["Hri"] = barcode.Hri.ToString(),
                ["Data"] = barcode.Data
            },
            barcode.RawBytes,
            barcode.LengthInBytes,
            GetDescription(barcode)));
    }

    private static void AddBarcodeVisualElement(PrintBarcode barcode, List<BaseElement> viewElements)
    {
        // Calculate actual rendered dimensions based on rotation
        var (renderedWidth, renderedHeight) = CalculateRotatedDimensions(
            barcode.Width,
            barcode.Height,
            barcode.Rotation);

        // Barcodes are represented as image elements with placeholder media
        viewElements.Add(new ImageElement(
            new LayoutMedia(
                "image/barcode",
                0,
                string.Empty,
                string.Empty),
            barcode.X,
            barcode.Y,
            renderedWidth,
            renderedHeight,
            EplRotationMapper.ToDomainRotation(barcode.Rotation)));
    }

    private static void AddDrawLineDebugElement(DrawBox drawBox, List<BaseElement> debugElements)
    {
        debugElements.Add(new DebugInfo(
            "drawLine",
            new Dictionary<string, string>
            {
                ["X1"] = drawBox.X1.ToString(),
                ["Y1"] = drawBox.Y1.ToString(),
                ["Thickness"] = drawBox.Thickness.ToString(),
                ["X2"] = drawBox.X2.ToString(),
                ["Y2"] = drawBox.Y2.ToString()
            },
            drawBox.RawBytes,
            drawBox.LengthInBytes,
            GetDescription(drawBox)));
    }

    private static void AddDrawLineVisualElement(DrawBox drawBox, List<BaseElement> viewElements)
    {
        viewElements.Add(new LineElement(
            drawBox.X1,
            drawBox.Y1,
            drawBox.X2,
            drawBox.Y2,
            drawBox.Thickness));
    }

    private static void AddRasterImageDebugElement(EplRasterImage rasterImage, List<BaseElement> debugElements)
    {
        debugElements.Add(new DebugInfo(
            "rasterImage",
            new Dictionary<string, string>
            {
                ["X"] = rasterImage.X.ToString(),
                ["Y"] = rasterImage.Y.ToString(),
                ["Width"] = rasterImage.Width.ToString(),
                ["Height"] = rasterImage.Height.ToString(),
                ["ContentType"] = rasterImage.Media.ContentType,
                ["Url"] = rasterImage.Media.Url
            },
            rasterImage.RawBytes,
            rasterImage.LengthInBytes,
            GetDescription(rasterImage)));
    }

    private static void AddRasterImageVisualElement(EplRasterImage rasterImage, List<BaseElement> viewElements)
    {
        // Raster images are represented as image elements with actual media
        viewElements.Add(new ImageElement(
            new LayoutMedia(
                rasterImage.Media.ContentType,
                ToMediaSize(rasterImage.Media.Length),
                rasterImage.Media.Url,
                rasterImage.Media.Sha256Checksum),
            rasterImage.X,
            rasterImage.Y,
            rasterImage.Width,
            rasterImage.Height,
            Rotation.None));
    }

    private static void AddBarcodeDebugElement(EplPrintBarcode barcode, List<BaseElement> debugElements)
    {
        debugElements.Add(new DebugInfo(
            "printBarcode",
            new Dictionary<string, string>
            {
                ["X"] = barcode.X.ToString(),
                ["Y"] = barcode.Y.ToString(),
                ["Rotation"] = barcode.Rotation.ToString(),
                ["Type"] = barcode.Type,
                ["Width"] = barcode.Width.ToString(),
                ["Height"] = barcode.Height.ToString(),
                ["Hri"] = barcode.Hri.ToString(),
                ["Data"] = barcode.Data,
                ["ContentType"] = barcode.Media.ContentType,
                ["Url"] = barcode.Media.Url
            },
            barcode.RawBytes,
            barcode.LengthInBytes,
            GetDescription(barcode)));
    }

    private static void AddBarcodeVisualElement(EplPrintBarcode barcode, List<BaseElement> viewElements)
    {
        // Calculate actual rendered dimensions based on rotation
        var (renderedWidth, renderedHeight) = CalculateRotatedDimensions(
            barcode.Width,
            barcode.Height,
            barcode.Rotation);

        // Barcodes are represented as image elements with actual media
        viewElements.Add(new ImageElement(
            new LayoutMedia(
                barcode.Media.ContentType,
                ToMediaSize(barcode.Media.Length),
                barcode.Media.Url,
                barcode.Media.Sha256Checksum),
            barcode.X,
            barcode.Y,
            renderedWidth,
            renderedHeight,
            EplRotationMapper.ToDomainRotation(barcode.Rotation)));
    }

    private static void AddLegacyBarcodeDebugElement(PrintBarcode barcode, List<BaseElement> debugElements)
    {
        debugElements.Add(new DebugInfo(
            "printBarcode",
            new Dictionary<string, string>
            {
                ["X"] = barcode.X.ToString(),
                ["Y"] = barcode.Y.ToString(),
                ["Rotation"] = barcode.Rotation.ToString(),
                ["Type"] = barcode.Type,
                ["Width"] = barcode.Width.ToString(),
                ["Height"] = barcode.Height.ToString(),
                ["Hri"] = barcode.Hri.ToString(),
                ["Data"] = barcode.Data
            },
            barcode.RawBytes,
            barcode.LengthInBytes,
            GetDescription(barcode)));
    }

    private static void AddLegacyBarcodeVisualElement(PrintBarcode barcode, List<BaseElement> viewElements)
    {
        // Calculate actual rendered dimensions based on rotation
        var (renderedWidth, renderedHeight) = CalculateRotatedDimensions(
            barcode.Width,
            barcode.Height,
            barcode.Rotation);

        // Barcodes are represented as image elements with placeholder media
        viewElements.Add(new ImageElement(
            new LayoutMedia(
                "image/barcode",
                0,
                string.Empty,
                string.Empty),
            barcode.X,
            barcode.Y,
            renderedWidth,
            renderedHeight,
            EplRotationMapper.ToDomainRotation(barcode.Rotation)));
    }

    private static int ToMediaSize(long length)
    {
        // Clamp to int to satisfy layout metadata without overflowing.
        return length > int.MaxValue ? int.MaxValue : (int)length;
    }

    private static (int Width, int Height) GetFontDimensions(int font)
    {
        return (
            EplSpecs.Fonts.GetBaseWidth(font),
            EplSpecs.Fonts.GetBaseHeight(font));
    }

    private static string GetFontName(int font) =>
        EplSpecs.Fonts.GetName(font);

    private static (int Width, int Height) CalculateRotatedDimensions(int width, int height, int rotation)
    {
        return rotation switch
        {
            0 => (width, height),    // Normal
            1 => (height, width),    // 90° clockwise
            2 => (width, height),    // 180°
            3 => (height, width),    // 270° clockwise
            _ => (width, height)
        };
    }

    private static Encoding GetEncodingFromCodePage(int p1, int p2, int p3)
    {
        try
        {
            return p1 switch
            {
                0 or 8 => Encoding.GetEncoding(866), // DOS 866 Cyrillic
                _ => Encoding.GetEncoding(437)       // Default to CP437
            };
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return Encoding.GetEncoding(437);
        }
    }

    private sealed record PrintCommandInfo(int Index, int Copies, int UnprintedVisualCount);

    private sealed class RenderState
    {
        public Encoding CurrentEncoding { get; set; } = Encoding.GetEncoding(437);

        public static RenderState CreateDefault() => new();
    }
}
