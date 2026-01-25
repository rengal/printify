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
/// Renders EPL protocol commands to a canvas.
/// </summary>
public sealed class EplRenderer : IRenderer
{
    public Canvas Render(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Protocol != Protocol.Epl)
        {
            throw new BadRequestException(
                $"EplRenderer only supports Epl protocol, got {document.Protocol}.");
        }

        var state = RenderState.CreateDefault();
        var items = new List<BaseElement>();

        foreach (var command in document.Commands)
        {
            switch (command)
            {
                case ScalableText scalableText:
                    AddScalableTextElement(scalableText, state, items);
                    break;

                case DrawHorizontalLine horizontalLine:
                    AddDrawHorizontalLineElement(horizontalLine, items);
                    break;

                case Print print:
                    items.Add(new DebugInfo(
                        "print",
                        new Dictionary<string, string>
                        {
                            ["Copies"] = print.Copies.ToString()
                        },
                        print.RawBytes,
                        print.LengthInBytes,
                        CommandDescriptionBuilder.Build(print)));
                    break;

                case PrintBarcode barcode:
                    AddBarcodeElement(barcode, items);
                    break;

                case PrintGraphic graphic:
                    AddGraphicElement(graphic, items);
                    break;

                case DrawLine drawLine:
                    AddDrawLineElement(drawLine, items);
                    break;

                case ClearBuffer:
                    items.Add(new DebugInfo(
                        "clearBuffer",
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    break;

                case SetLabelWidth labelWidth:
                    items.Add(new DebugInfo(
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
                    items.Add(new DebugInfo(
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
                    items.Add(new DebugInfo(
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
                    items.Add(new DebugInfo(
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
                    items.Add(new DebugInfo(
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
                    state.CurrentEncoding = GetEncodingFromCodePage(intlChar.Code);
                    items.Add(new DebugInfo(
                        "setInternationalCharacter",
                        new Dictionary<string, string>
                        {
                            ["Code"] = intlChar.Code.ToString()
                        },
                        intlChar.RawBytes,
                        intlChar.LengthInBytes,
                        CommandDescriptionBuilder.Build(intlChar)));
                    break;

                case SetCodePage codePage:
                    state.CurrentEncoding = GetEncodingFromCodePage(codePage.Code, codePage.Scaling);
                    items.Add(new DebugInfo(
                        "setCodePage",
                        new Dictionary<string, string>
                        {
                            ["Code"] = codePage.Code.ToString(),
                            ["Scaling"] = codePage.Scaling.ToString()
                        },
                        codePage.RawBytes,
                        codePage.LengthInBytes,
                        CommandDescriptionBuilder.Build(codePage)));
                    break;

                case ParseError error:
                    items.Add(new DebugInfo(
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
                    items.Add(new DebugInfo(
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
                    items.Add(new DebugInfo(
                        command.GetType().Name,
                        new Dictionary<string, string>(),
                        command.RawBytes,
                        command.LengthInBytes,
                        CommandDescriptionBuilder.Build(command)));
                    break;
            }
        }

        return new Canvas(
            WidthInDots: document.WidthInDots,
            HeightInDots: document.HeightInDots,
            Items: items.AsReadOnly());
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
        var width = baseWidth * scalableText.HorizontalMultiplication;
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

    private static Encoding GetEncodingFromCodePage(int code, int scaling = 0)
    {
        try
        {
            return code switch
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

    private sealed class RenderState
    {
        public Encoding CurrentEncoding { get; set; } = Encoding.GetEncoding(437);

        public static RenderState CreateDefault() => new();
    }
}
