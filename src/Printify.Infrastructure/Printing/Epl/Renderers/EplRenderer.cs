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
        var allItems = new List<BaseElement>();
        var printCommands = new List<PrintCommandInfo>();

        // First pass: collect all items and track Print command positions
        foreach (var command in document.Commands)
        {
            switch (command)
            {
                case ScalableText scalableText:
                    AddScalableTextElement(scalableText, state, allItems);
                    break;

                case DrawHorizontalLine horizontalLine:
                    AddDrawHorizontalLineElement(horizontalLine, allItems);
                    break;

                case Print print:
                    allItems.Add(new DebugInfo(
                        "print",
                        new Dictionary<string, string>
                        {
                            ["Copies"] = print.Copies.ToString()
                        },
                        print.RawBytes,
                        print.LengthInBytes,
                        CommandDescriptionBuilder.Build(print)));
                    // Track this print command position
                    printCommands.Add(new PrintCommandInfo(allItems.Count - 1, print.Copies));
                    break;

                case PrintBarcode barcode:
                    AddBarcodeElement(barcode, allItems);
                    break;

                case PrintGraphic graphic:
                    AddGraphicElement(graphic, allItems);
                    break;

                case DrawLine drawLine:
                    AddDrawLineElement(drawLine, allItems);
                    break;

                case ClearBuffer:
                    allItems.Add(new DebugInfo(
                        "clearBuffer",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    break;

                case CarriageReturn:
                    allItems.Add(new DebugInfo(
                        "carriageReturn",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    break;

                case LineFeed:
                    allItems.Add(new DebugInfo(
                        "lineFeed",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    break;

                case SetLabelWidth labelWidth:
                    allItems.Add(new DebugInfo(
                        "setLabelWidth",
                        new Dictionary<string, string>
                        {
                            ["Width"] = labelWidth.Width.ToString()
                        },
                        labelWidth.RawBytes,
                        labelWidth.LengthInBytes,
                        CommandDescriptionBuilder.Build(labelWidth)));
                    break;

                case SetLabelHeight labelHeight:
                    allItems.Add(new DebugInfo(
                        "setLabelHeight",
                        new Dictionary<string, string>
                        {
                            ["Height"] = labelHeight.Height.ToString(),
                            ["SecondParameter"] = labelHeight.SecondParameter.ToString()
                        },
                        labelHeight.RawBytes,
                        labelHeight.LengthInBytes,
                        CommandDescriptionBuilder.Build(labelHeight)));
                    break;

                case SetPrintSpeed speed:
                    allItems.Add(new DebugInfo(
                        "setPrintSpeed",
                        new Dictionary<string, string>
                        {
                            ["Speed"] = speed.Speed.ToString()
                        },
                        speed.RawBytes,
                        speed.LengthInBytes,
                        CommandDescriptionBuilder.Build(speed)));
                    break;

                case SetPrintDarkness darkness:
                    allItems.Add(new DebugInfo(
                        "setPrintDarkness",
                        new Dictionary<string, string>
                        {
                            ["Darkness"] = darkness.Darkness.ToString()
                        },
                        darkness.RawBytes,
                        darkness.LengthInBytes,
                        CommandDescriptionBuilder.Build(darkness)));
                    break;

                case SetPrintDirection direction:
                    allItems.Add(new DebugInfo(
                        "setPrintDirection",
                        new Dictionary<string, string>
                        {
                            ["Direction"] = direction.Direction.ToString()
                        },
                        direction.RawBytes,
                        direction.LengthInBytes,
                        CommandDescriptionBuilder.Build(direction)));
                    break;

                case SetInternationalCharacter intlChar:
                    state.CurrentEncoding = GetEncodingFromCodePage(intlChar.P1, intlChar.P2, intlChar.P3);
                    allItems.Add(new DebugInfo(
                        "setInternationalCharacter",
                        new Dictionary<string, string>
                        {
                            ["P1"] = intlChar.P1.ToString(),
                            ["P2"] = intlChar.P2.ToString(),
                            ["P3"] = intlChar.P3.ToString()
                        },
                        intlChar.RawBytes,
                        intlChar.LengthInBytes,
                        CommandDescriptionBuilder.Build(intlChar)));
                    break;

                case ParseError error:
                    allItems.Add(new DebugInfo(
                        "error",
                        new Dictionary<string, string>
                        {
                            ["Code"] = error.Code ?? string.Empty,
                            ["Message"] = error.Message ?? "Unknown error"
                        },
                        error.RawBytes,
                        error.LengthInBytes,
                        CommandDescriptionBuilder.Build(error)));
                    break;

                case PrinterError printerError:
                    allItems.Add(new DebugInfo(
                        "printerError",
                        new Dictionary<string, string>
                        {
                            ["Message"] = printerError.Message ?? "Printer error"
                        },
                        printerError.RawBytes,
                        printerError.LengthInBytes,
                        CommandDescriptionBuilder.Build(printerError)));
                    break;

                default:
                    allItems.Add(new DebugInfo(
                        command.GetType().Name,
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    break;
            }
        }

        // If no print commands, return single canvas with all items (debug only + error)
        if (printCommands.Count == 0)
        {
            return new[]
            {
                new Canvas(
                    WidthInDots: document.WidthInDots,
                    HeightInDots: document.HeightInDots,
                    Items: allItems.AsReadOnly())
            };
        }

        // Generate canvases based on print commands
        var canvases = new List<Canvas>();
        var startIndex = 0;

        foreach (var printCmd in printCommands)
        {
            // Items for this segment (from after previous print to this print)
            var segmentItems = allItems.GetRange(startIndex, printCmd.Index - startIndex);

            // Extract visual elements (non-debug elements) from this segment
            var visualElements = segmentItems
                .Where(item => item is not DebugInfo)
                .Select(CloneVisualElement)
                .ToList();

            // Create copies based on print command copies count
            for (int copy = 0; copy < printCmd.Copies; copy++)
            {
                if (copy == 0 && startIndex == 0)
                {
                    // First canvas: all items (debug + visual)
                    canvases.Add(new Canvas(
                        WidthInDots: document.WidthInDots,
                        HeightInDots: document.HeightInDots,
                        Items: segmentItems.Concat(visualElements).ToList().AsReadOnly()));
                }
                else
                {
                    // Subsequent canvases: only visual elements (no debug)
                    canvases.Add(new Canvas(
                        WidthInDots: document.WidthInDots,
                        HeightInDots: document.HeightInDots,
                        Items: visualElements.ToList().AsReadOnly()));
                }
            }

            startIndex = printCmd.Index + 1;
        }

        // If there are items after the last print command, create a final canvas with debug only
        if (startIndex < allItems.Count)
        {
            var remainingItems = allItems.GetRange(startIndex, allItems.Count - startIndex);
            canvases.Add(new Canvas(
                WidthInDots: document.WidthInDots,
                HeightInDots: document.HeightInDots,
                Items: remainingItems.AsReadOnly()));
        }

        return canvases.ToArray();
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

    private static void AddScalableTextElement(ScalableText scalableText, RenderState state, List<BaseElement> items)
    {
        // Decode raw bytes using current codepage
        var decodedText = state.CurrentEncoding.GetString(scalableText.TextBytes);

        // Add debug element for the command
        items.Add(new DebugInfo(
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
            CommandDescriptionBuilder.Build(scalableText)));

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
        items.Add(new TextElement(
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

    private static void AddDrawHorizontalLineElement(DrawHorizontalLine horizontalLine, List<BaseElement> items)
    {
        items.Add(new DebugInfo(
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
            CommandDescriptionBuilder.Build(horizontalLine)));

        // Horizontal lines are represented as text elements for rendering
        items.Add(new TextElement(
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

    private static void AddBarcodeElement(PrintBarcode barcode, List<BaseElement> items)
    {
        items.Add(new DebugInfo(
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
            CommandDescriptionBuilder.Build(barcode)));

        // Calculate actual rendered dimensions based on rotation
        var (renderedWidth, renderedHeight) = CalculateRotatedDimensions(
            barcode.Width,
            barcode.Height,
            barcode.Rotation);

        // Barcodes are represented as image elements with placeholder media
        items.Add(new ImageElement(
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

    private static void AddGraphicElement(PrintGraphic graphic, List<BaseElement> items)
    {
        items.Add(new DebugInfo(
            "printGraphic",
            new Dictionary<string, string>
            {
                ["X"] = graphic.X.ToString(),
                ["Y"] = graphic.Y.ToString(),
                ["Width"] = graphic.Width.ToString(),
                ["Height"] = graphic.Height.ToString(),
                ["DataLength"] = graphic.Data.Length.ToString()
            },
            graphic.RawBytes,
            graphic.LengthInBytes,
            CommandDescriptionBuilder.Build(graphic)));

        // Graphics are represented as image elements
        items.Add(new ImageElement(
            new LayoutMedia(
                "image/epl-graphic",
                graphic.Data.Length,
                string.Empty,
                string.Empty),
            graphic.X,
            graphic.Y,
            graphic.Width,
            graphic.Height,
            0));
    }

    private static void AddDrawLineElement(DrawLine drawLine, List<BaseElement> items)
    {
        items.Add(new DebugInfo(
            "drawLine",
            new Dictionary<string, string>
            {
                ["X1"] = drawLine.X1.ToString(),
                ["Y1"] = drawLine.Y1.ToString(),
                ["Thickness"] = drawLine.Thickness.ToString(),
                ["X2"] = drawLine.X2.ToString(),
                ["Y2"] = drawLine.Y2.ToString()
            },
            drawLine.RawBytes,
            drawLine.LengthInBytes,
            CommandDescriptionBuilder.Build(drawLine)));

        // Lines are represented as text elements for rendering
        // The bounding box is calculated from the line endpoints
        var x = Math.Min(drawLine.X1, drawLine.X2);
        var y = Math.Min(drawLine.Y1, drawLine.Y2);
        var width = Math.Abs(drawLine.X2 - drawLine.X1);
        var height = Math.Abs(drawLine.Y2 - drawLine.Y1);

        items.Add(new TextElement(
            string.Empty,
            x,
            y,
            Math.Max(width, drawLine.Thickness),
            Math.Max(height, drawLine.Thickness),
            null,
            0,
            false,
            false,
            false,
            1,
            1,
            0));
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

    private sealed record PrintCommandInfo(int Index, int Copies);

    private sealed class RenderState
    {
        public Encoding CurrentEncoding { get; set; } = Encoding.GetEncoding(437);

        public static RenderState CreateDefault() => new();
    }
}
