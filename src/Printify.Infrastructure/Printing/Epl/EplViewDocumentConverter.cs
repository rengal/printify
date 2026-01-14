using Printify.Application.Exceptions;
using Printify.Application.Features.Printers.Documents;
using Printify.Application.Features.Printers.Documents.View;
using Printify.Domain.Documents;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Documents.Elements.Epl;
using Printify.Domain.Documents.View;
using Printify.Domain.Printers;

namespace Printify.Infrastructure.Printing.Epl;

public sealed class EplViewDocumentConverter : IViewDocumentConverter
{
    public ViewDocument ToViewDocument(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Protocol != Protocol.Epl)
        {
            throw new BadRequestException(
                $"View conversion is only supported for EPL, got {document.Protocol}.");
        }

        var elements = new List<ViewElement>();

        foreach (var element in document.Elements)
        {
            switch (element)
            {
                case ScalableText scalableText:
                    AddScalableTextViewElement(scalableText, elements);
                    break;
                case DrawHorizontalLine horizontalLine:
                    AddDrawHorizontalLineViewElement(horizontalLine, elements);
                    break;
                case Print print:
                    AddDebugElement(elements, print, "print", new Dictionary<string, string>
                    {
                        ["Copies"] = print.Copies.ToString()
                    });
                    break;
                case PrintBarcode barcode:
                    AddBarcodeViewElement(barcode, elements);
                    break;
                case PrintGraphic graphic:
                    AddGraphicViewElement(graphic, elements);
                    break;
                case DrawLine drawLine:
                    AddDrawLineViewElement(drawLine, elements);
                    break;
                case ClearBuffer:
                    AddDebugElement(elements, element, "clearBuffer", new Dictionary<string, string>());
                    break;
                case SetLabelWidth labelWidth:
                    AddDebugElement(elements, labelWidth, "setLabelWidth", new Dictionary<string, string>
                    {
                        ["Width"] = labelWidth.Width.ToString()
                    });
                    break;
                case SetLabelHeight labelHeight:
                    AddDebugElement(elements, labelHeight, "setLabelHeight", new Dictionary<string, string>
                    {
                        ["Height"] = labelHeight.Height.ToString(),
                        ["SecondParameter"] = labelHeight.SecondParameter.ToString()
                    });
                    break;
                case SetPrintSpeed speed:
                    AddDebugElement(elements, speed, "setPrintSpeed", new Dictionary<string, string>
                    {
                        ["Speed"] = speed.Speed.ToString()
                    });
                    break;
                case SetPrintDarkness darkness:
                    AddDebugElement(elements, darkness, "setPrintDarkness", new Dictionary<string, string>
                    {
                        ["Darkness"] = darkness.Darkness.ToString()
                    });
                    break;
                case SetPrintDirection direction:
                    AddDebugElement(elements, direction, "setPrintDirection", new Dictionary<string, string>
                    {
                        ["Direction"] = direction.Direction.ToString()
                    });
                    break;
                case SetInternationalCharacter intlChar:
                    AddDebugElement(elements, intlChar, "setInternationalCharacter", new Dictionary<string, string>
                    {
                        ["Code"] = intlChar.Code.ToString()
                    });
                    break;
                case SetCodePage codePage:
                    AddDebugElement(elements, codePage, "setCodePage", new Dictionary<string, string>
                    {
                        ["Code"] = codePage.Code.ToString(),
                        ["Scaling"] = codePage.Scaling.ToString()
                    });
                    break;
                case ParseError error:
                    AddDebugElement(elements, error, "error", new Dictionary<string, string>
                    {
                        ["Code"] = error.Code ?? string.Empty,
                        ["Message"] = error.Message ?? "Unknown error"
                    });
                    break;
                case PrinterError printerError:
                    AddDebugElement(elements, printerError, "printerError", new Dictionary<string, string>
                    {
                        ["Message"] = printerError.Message ?? "Printer error"
                    });
                    break;
                default:
                    AddDebugElement(elements, element, element.GetType().Name, new Dictionary<string, string>());
                    break;
            }
        }

        // Collect error messages from ParseError and PrinterError elements
        var errorMessages = document.Elements
            .Where(e => e is ParseError or PrinterError)
            .Select(e => e switch
            {
                ParseError error => error.Message ?? error.Code ?? "Unknown error",
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
            document.BytesReceived,
            document.BytesSent,
            elements.AsReadOnly(),
            errorMessages is { Length: > 0 } ? errorMessages : null);
    }

    private static void AddScalableTextViewElement(ScalableText scalableText, List<ViewElement> elements)
    {
        // Add debug element for the command
        AddDebugElement(elements, scalableText, "scalableText", new Dictionary<string, string>
        {
            ["X"] = scalableText.X.ToString(),
            ["Y"] = scalableText.Y.ToString(),
            ["Rotation"] = scalableText.Rotation.ToString(),
            ["Font"] = scalableText.Font.ToString(),
            ["HorizontalMultiplication"] = scalableText.HorizontalMultiplication.ToString(),
            ["VerticalMultiplication"] = scalableText.VerticalMultiplication.ToString(),
            ["Reverse"] = scalableText.Reverse.ToString(),
            ["Text"] = scalableText.Text
        });

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
        elements.Add(new ViewTextElement(
            scalableText.Text,
            scalableText.X,
            scalableText.Y,
            renderedWidth,
            renderedHeight,
            GetFontName(scalableText.Font),
            0, // CharSpacing not applicable for EPL scalable text
            scalableText.Reverse == 'R', // IsBold - we use reverse as a style indicator
            false, // IsUnderline
            scalableText.Reverse == 'R') // IsReverse
        {
            CommandRaw = scalableText.CommandRaw,
            CommandDescription = CommandDescriptionBuilder.Build(scalableText),
            LengthInBytes = scalableText.LengthInBytes,
            CharScaleX = scalableText.HorizontalMultiplication,
            CharScaleY = scalableText.VerticalMultiplication,
            ZIndex = 0
        });
    }

    private static void AddDrawHorizontalLineViewElement(DrawHorizontalLine horizontalLine, List<ViewElement> elements)
    {
        AddDebugElement(elements, horizontalLine, "drawHorizontalLine", new Dictionary<string, string>
        {
            ["X"] = horizontalLine.X.ToString(),
            ["Y"] = horizontalLine.Y.ToString(),
            ["Thickness"] = horizontalLine.Thickness.ToString(),
            ["Length"] = horizontalLine.Length.ToString()
        });

        // Horizontal lines are represented as text elements for rendering
        elements.Add(new ViewTextElement(
            string.Empty, // No actual text content
            horizontalLine.X,
            horizontalLine.Y,
            horizontalLine.Length,
            horizontalLine.Thickness,
            null, // No font
            0,
            false,
            false,
            false)
        {
            CommandRaw = horizontalLine.CommandRaw,
            CommandDescription = CommandDescriptionBuilder.Build(horizontalLine),
            LengthInBytes = horizontalLine.LengthInBytes,
            ZIndex = 0
        });
    }

    private static void AddBarcodeViewElement(PrintBarcode barcode, List<ViewElement> elements)
    {
        AddDebugElement(elements, barcode, "printBarcode", new Dictionary<string, string>
        {
            ["X"] = barcode.X.ToString(),
            ["Y"] = barcode.Y.ToString(),
            ["Rotation"] = barcode.Rotation.ToString(),
            ["Type"] = barcode.Type,
            ["Width"] = barcode.Width.ToString(),
            ["Height"] = barcode.Height.ToString(),
            ["Hri"] = barcode.Hri.ToString(),
            ["Data"] = barcode.Data
        });

        // Calculate actual rendered dimensions based on rotation
        var (renderedWidth, renderedHeight) = CalculateRotatedDimensions(
            barcode.Width,
            barcode.Height,
            barcode.Rotation);

        // Barcodes are represented as image elements with placeholder media
        // The actual barcode image would be generated during rendering
        var media = new ViewMedia(
            "image/barcode",
            0,
            string.Empty,
            string.Empty);

        elements.Add(new ViewImageElement(
            media,
            barcode.X,
            barcode.Y,
            renderedWidth,
            renderedHeight)
        {
            CommandRaw = barcode.CommandRaw,
            CommandDescription = CommandDescriptionBuilder.Build(barcode),
            LengthInBytes = barcode.LengthInBytes,
            ZIndex = 0
        });
    }

    private static void AddGraphicViewElement(PrintGraphic graphic, List<ViewElement> elements)
    {
        AddDebugElement(elements, graphic, "printGraphic", new Dictionary<string, string>
        {
            ["X"] = graphic.X.ToString(),
            ["Y"] = graphic.Y.ToString(),
            ["Width"] = graphic.Width.ToString(),
            ["Height"] = graphic.Height.ToString(),
            ["DataLength"] = graphic.Data.Length.ToString()
        });

        // Graphics are represented as image elements
        var media = new ViewMedia(
            "image/epl-graphic",
            graphic.Data.Length,
            string.Empty,
            string.Empty);

        elements.Add(new ViewImageElement(
            media,
            graphic.X,
            graphic.Y,
            graphic.Width,
            graphic.Height)
        {
            CommandRaw = graphic.CommandRaw,
            CommandDescription = CommandDescriptionBuilder.Build(graphic),
            LengthInBytes = graphic.LengthInBytes,
            ZIndex = 0
        });
    }

    private static void AddDrawLineViewElement(DrawLine drawLine, List<ViewElement> elements)
    {
        AddDebugElement(elements, drawLine, "drawLine", new Dictionary<string, string>
        {
            ["X1"] = drawLine.X1.ToString(),
            ["Y1"] = drawLine.Y1.ToString(),
            ["Thickness"] = drawLine.Thickness.ToString(),
            ["X2"] = drawLine.X2.ToString(),
            ["Y2"] = drawLine.Y2.ToString()
        });

        // Lines are represented as text elements for rendering
        // The bounding box is calculated from the line endpoints
        var x = Math.Min(drawLine.X1, drawLine.X2);
        var y = Math.Min(drawLine.Y1, drawLine.Y2);
        var width = Math.Abs(drawLine.X2 - drawLine.X1);
        var height = Math.Abs(drawLine.Y2 - drawLine.Y1);

        elements.Add(new ViewTextElement(
            string.Empty,
            x,
            y,
            Math.Max(width, drawLine.Thickness),
            Math.Max(height, drawLine.Thickness),
            null,
            0,
            false,
            false,
            false)
        {
            CommandRaw = drawLine.CommandRaw,
            CommandDescription = CommandDescriptionBuilder.Build(drawLine),
            LengthInBytes = drawLine.LengthInBytes,
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

    private static (int Width, int Height) GetFontDimensions(int font)
    {
        return font switch
        {
            2 => (EplViewConstants.Font0Width, EplViewConstants.Font0Height), // Font 0
            3 => (EplViewConstants.Font1Width, EplViewConstants.Font1Height), // Font 1
            4 => (EplViewConstants.Font2Width, EplViewConstants.Font2Height), // Font 2
            _ => (EplViewConstants.Font0Width, EplViewConstants.Font0Height)   // Default to Font 0
        };
    }

    private static string? GetFontName(int font)
    {
        return font switch
        {
            2 => ViewFontNames.EplFont0,
            3 => ViewFontNames.EplFont1,
            4 => ViewFontNames.EplFont2,
            _ => null
        };
    }

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
}
