using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Resto.Common.Agent.Drivers.PrinterDrivers;
using Resto.Data.Properties;
using Resto.Framework.Attributes.JetBrains;
using Resto.Framework.Common;
using Resto.Framework.Common.DependencyInjection;
using Resto.Framework.Common.Print.Image;
using Resto.Framework.Common.Print.VirtualTape.Drivers;
using Resto.Framework.Common.Print.VirtualTape.Encoders;
using Resto.Framework.Common.Print.VirtualTape.ParsedElements;
using Resto.Framework.Data;

// ReSharper disable once CheckNamespace
namespace Resto.Data;

public partial class ZebraEplDriver
{
    #region Fields and Properties

    [Transient]
    private static readonly Dictionary<string, string> CodepagesToCommandMapping = new()
    {
        { "437", "49 38 2C 30" }, // DOS 437 English - US
        { "850", "49 38 2C 31" }, // DOS 850 Latin 1
        { "852", "49 38 2C 33" }, // DOS 852 Latin 2 (Cyrillic II / Slavic)
        { "860", "49 38 2C 33" }, // DOS 860 Portuguese
        { "863", "49 38 2C 34" }, // DOS 863 French Canadian
        { "865", "49 38 2C 35" }, // DOS 865 Nordic
        { "857", "49 38 2C 36" }, // DOS 857 Turkish
        { "861", "49 38 2C 37" }, // DOS 861 Icelandic
        { "862", "49 38 2C 38" }, // DOS 862 Hebrew
        { "855", "49 38 2C 39" }, // DOS 855 Cyrillic
        { "866", "49 38 2C 31 30" }, // DOS 866 Cyrillic (CIS 1)
        { "737", "49 38 2C 31 31" }, // DOS 737 Greek
        { "851", "49 38 2C 31 32" }, // DOS 851 Greek 1
        { "869", "49 38 2C 31 33" }, // DOS 869 Greek 2
        { "1252", "49 38 2C 41" }, // Windows 1252 Latin 1
        { "1250", "49 38 2C 42" }, // Windows 1250 Latin 2
        { "1251", "49 38 2C 43" }, // Windows 1251 Cyrillic
        { "1253", "49 38 2C 44" }, // Windows 1253 Greek
        { "1254", "49 38 2C 45" }, // Windows 1254 Turkish
        { "1255", "49 38 2C 46" }  // Windows 1255 Hebrew
    };

    [Transient]
    private static readonly Dictionary<string, (char esc, byte horMultiply, byte vertMultiply)> fontsEsc = new()
    {
        { "f0", ('2', 1, 1)},
        { "f1", ('3', 1, 1)},
        { "f2", ('4', 1, 2)}
    };

    [Transient]
    // ReSharper disable once InconsistentNaming
    private Dictionary<string, ElementMetrics> fontSizes => PageSettings.Dpi == 203
        ? new Dictionary<string, ElementMetrics>
        {
            { "f0", new ElementMetrics(10, 14, 0, 2, 1, 8) },
            { "f1", new ElementMetrics(12, 16, 0, 2, 1, 8) },
            { "f2", new ElementMetrics(14, 40, 0, 2, 1, 8) }
        }
        : new Dictionary<string, ElementMetrics>
        {
            { "f0", new ElementMetrics(16, 28, 0, 2, 1, 8) },
            { "f1", new ElementMetrics(20, 36, 0, 2, 1, 8) },
            { "f2", new ElementMetrics(24, 44, 0, 2, 1, 8) }
        };

    [Transient]
    private readonly Dictionary<string, byte> qrModuleSizes = new()
    {
        { "tiny", 3 },
        { "small", 4 },
        { "normal", 6 },
        { "large", 8 },
        { "extralarge", 10 }
    };

    /// <summary>
    /// Отступ (в юнитах) вокруг qr-кода
    /// </summary>
    [Transient]
    private readonly Dictionary<string, byte> qrMarginInModules = new()
    {
        { "tiny", 2 },
        { "small", 1 },
        { "normal", 1 },
        { "large", 1 },
        { "extralarge", 1 }
    };

    [Transient]
    private PrintOrientation printOrientation;

    [Transient]
    public static readonly IPrinterPageSettings DefaultPageSettings = new PrinterPageSettings(
        printableWidth: 448,
        printableHeight: 301,
        dpi: 203,
        marginLeft: 1,
        marginRight: 1,
        marginTop: 0,
        marginBottom: 0);

    private static IImageHelper ImageHelper => DiHelper.Container.Resolve<IImageHelper>();

    #endregion

    #region AgentDriver implementation

    public override string Name => Resources.ZebraEplDriverName;

    public override string Description => Resources.ZebraEplDriverDescription;

    public override void InitPrintParams([CanBeNull] DeviceSettings settings, int? baseWidth)
    {
        base.InitPrintParams(settings, baseWidth);
        if (settings is PortWriterDriverSettings portWriterDriverSettings)
            printOrientation = portWriterDriverSettings.PageSettings?.PrintOrientation ?? PrintOrientation.DEFAULT;
        var contentAreaWidth = ContentAreaSize.width;
        font0Width = contentAreaWidth / (fontSizes["f0"].Width + fontSizes["f0"].MarginLeft + fontSizes["f0"].MarginRight);
        font1Width = contentAreaWidth / (fontSizes["f1"].Width + fontSizes["f1"].MarginLeft + fontSizes["f1"].MarginRight);
        font2Width = contentAreaWidth / (fontSizes["f2"].Width + fontSizes["f2"].MarginLeft + fontSizes["f2"].MarginRight);
    }

    #endregion

    #region IByteStreamDriver implementation

    public override MarkupEncodingType MarkupEncodingType => MarkupEncodingType.Label;
    public override bool CanPrintBarcode => true;
    public override bool CanPrintQrCode => true;
    public override bool CanPrintLogo => false;
    public override bool CanPrintImage => true;
    public override int? EffectiveWidthInDots => PageSettings.PrintableWidth;
    public override int? EffectiveHeightInDots => PageSettings.PrintableHeight;
    public override int? EffectiveMarginTopInDots => PageSettings.MarginTop;
    public override int? EffectiveDpi => PageSettings.Dpi;

    [Transient]
    public override Dictionary<string, string> DefaultCodePageCommandMapping => CodepagesToCommandMapping;

    [Transient]
    public override Dictionary<string, ElementMetrics> FontsSizes => fontSizes;

    public override IPrinterPageSettings GetDefaultPageSettings()
    {
        return DefaultPageSettings;
    }

    public override void StartDocument([NotNull] IPrintingLayoutContext context)
    {
        // Очищаем графический буфер принтера 
        context.Buffer.AddRange(context.Encoding.GetBytes("\nN\n"));

        if (printOrientation == PrintOrientation.TOP_TO_BOTTOM)
            // Устанавливаем направление печати (сверху вниз)
            context.Buffer.AddRange(context.Encoding.GetBytes("ZT\n"));
        else if (printOrientation == PrintOrientation.BOTTOM_TO_TOP)
            // Устанавливаем направление печати (снизу вверх)
            context.Buffer.AddRange(context.Encoding.GetBytes("ZB\n"));

        //Устанавливаем размер этикетки
        context.Buffer.AddRange(context.Encoding.GetBytes(string.Format(CultureInfo.InvariantCulture, "q{0}\nQ{1},26\n",
            PageSettings.PrintableWidth, PageSettings.PrintableHeight)));
    }

    protected override ElementMetrics ProcessText([NotNull] IPrintingLayoutContext context, TextElement textElement, bool calculateOnly)
    {
        if (!context.Cache.IsSet)
        {
            var fontWidth = fontSizes[textElement.FontId].Width;
            var fontHeight = fontSizes[textElement.FontId].Height;
            var trimmedLen = textElement.Text.Trim().Length;

            var textMarginTop = context.PosY <= PageSettings.MarginTop
                ? PageSettings.MarginTop - context.PosY
                : FontsSizes[textElement.FontId].MarginTop;

            var fontMarginBottom = FontsSizes[textElement.FontId].MarginBottom;
            if (textElement.IsUnderline)
                fontMarginBottom += 2;

            context.Cache.Set(new ElementMetrics(fontWidth * trimmedLen, fontHeight, 0, 0, textMarginTop, fontMarginBottom));
        }

        var metrics = context.Cache.Metrics;
        
        if (!calculateOnly)
        {
            //RMS-54441 workaround: если в тексте есть двойные кавычки и кириллица - ломается кодировка. Поэтому если найдена кириллица заменяем двойные кавычки на одинарные
            var vertMultiply = fontsEsc[textElement.FontId].vertMultiply;
            var fontWidthWithMargin = fontSizes[textElement.FontId].Width + fontSizes[textElement.FontId].MarginRight;

            var trimmedString = textElement.Text.Trim();
            var indexStart = textElement.Text.IndexOf(trimmedString, StringComparison.Ordinal);
            var trimmedLen = trimmedString.Length;
            var posX = PageSettings.MarginLeft + fontWidthWithMargin * indexStart;
            var hasCyrillicLetters = Regex.IsMatch(textElement.Text, @"\p{IsCyrillic}");

            var escapedTrimmedString = trimmedString;
            //Если есть кириллица - заменяем " на ', иначе происходят ошибки с кодировкой (RMS-54441)
            if (hasCyrillicLetters)
                escapedTrimmedString = escapedTrimmedString.Replace('"', '\'');
            // меняем \ на \\ 
            escapedTrimmedString = escapedTrimmedString.Replace("\\", "\\\\");
            // меняем " на \"
            escapedTrimmedString = escapedTrimmedString.Replace("\"", "\\\"");

            if (trimmedLen > 0)
            {
                var cmd = string.Format(CultureInfo.InvariantCulture, "A{0},{1},0,{2},1,{3},{4},\"{5}\"\n",
                    posX,
                    context.PosY + metrics.MarginTop,
                    fontsEsc[textElement.FontId].esc,
                    vertMultiply,
                    textElement.IsReverse ? 'R' : 'N',
                    escapedTrimmedString);
                context.Buffer.AddRange(context.Encoding.GetBytes(cmd));

                if (textElement.IsUnderline)
                {
                    //Print horizontal line
                    context.Buffer.AddRange(context.Encoding.GetBytes($"LO{posX},{context.PosY + metrics.MarginTop + metrics.Height + 2},{fontWidthWithMargin * trimmedLen},2\n"));
                }
            }
        }

        return metrics;
    }

    public override ElementMetrics ProcessFeedLines([NotNull] IPrintingLayoutContext context, int linesCount, bool calculateOnly)
    {
        if (linesCount == 0)
            return ElementMetrics.Empty;

        var fontSize = FontsSizes["f0"];
        return new ElementMetrics(fontSize.Width, fontSize.Height * linesCount);
    }

    protected override ElementMetrics ProcessBarcode([NotNull] IPrintingLayoutContext context, BarcodeElement barcodeElement, bool calculateOnly)
    {
        if (ImageHelper == null)
            return ElementMetrics.Empty;

        const int barcodeMarginVert = 3;
        const int moduleWidth = 2;

        if (!context.Cache.IsSet)
            context.Cache.Set(ImageHelper.CalcEan13BarcodeSize(barcodeElement, moduleWidth).AddMarginsVert(barcodeMarginVert, barcodeMarginVert));

        var metrics = context.Cache.Metrics;

        if (!calculateOnly)
        {
            var posX = PrinterDriverHelper.GetPositionX(PageSettings, metrics.Width, barcodeElement.Align, false);

            var cmd = string.Format(CultureInfo.InvariantCulture, "B{0},{1},0,E30,{2},0,{3},{4},\"{5}\"\n",
                posX,
                context.PosY + barcodeMarginVert,
                moduleWidth,
                barcodeElement.IsHri
                    ? metrics.Height - barcodeMarginVert - fontSizes["f1"].Height
                    : metrics.Height - barcodeMarginVert,
                barcodeElement.IsHri ? 'B' : 'N',
                barcodeElement.Barcode);
            context.Buffer.AddRange(context.Encoding.GetBytes(cmd));
        }

        return metrics;
    }

    protected override ElementMetrics ProcessQrCode([NotNull] IPrintingLayoutContext context, QrCodeElement qrCodeElement, bool calculateOnly)
    {
        if (ImageHelper == null)
            return ElementMetrics.Empty;

        if (!context.Cache.IsSet)
        {
            var moduleSize = qrModuleSizes[qrCodeElement.Size];
            var marginInModules = qrMarginInModules[qrCodeElement.Size];
            var qrSize = ImageHelper.CalcQrCodeSizeWithMargins(qrCodeElement, moduleSize, marginInModules);
            var qrImage = ImageHelper.CreateQrCodeFromElement(qrCodeElement, qrSize.Width, 0, null);
            context.Cache.Set(qrImage);
        }

        var image = context.Cache.Image;

        if (image == null)
            return ElementMetrics.Empty;

        if (!calculateOnly)
            WriteImage(context, image, PageSettings, qrCodeElement.Align);

        return image.Metrics;
    }

    protected override ElementMetrics ProcessImage(IPrintingLayoutContext context, ImageElement imageElement, bool calculateOnly)
    {
        if (ImageHelper == null)
            return ElementMetrics.Empty;

        var contentAreaSize = ContentAreaSize;
        if (contentAreaSize.width <= 0)
            return ElementMetrics.Empty;

        if (!context.Cache.IsSet)
            context.Cache.Set(ImageHelper.CreateImageFromElement(imageElement, contentAreaSize, false));

        var image = context.Cache.Image;

        if (image == null)
            return ElementMetrics.Empty;

        if (!calculateOnly)
            WriteImage(context, image, PageSettings, imageElement.Align);

        return image.Metrics;
    }

    public override void ProcessPagecut([NotNull] IPrintingLayoutContext context)
    {
        context.Buffer.AddRange(context.Encoding.GetBytes("P1\n"));
        context.Buffer.AddRange(context.Encoding.GetBytes("\nN\n"));
    }

    public override void WriteRawCommand([NotNull] IPrintingLayoutContext context, [NotNull] byte[] data)
    {
        WriteRawCommandWithNewLine(context, data, [(byte)'\n']);
    }

    public override IEnumerable<IList<byte>> EndDocument([NotNull] IPrintingLayoutContext context)
    {
        if (context.HasPrintingOnPage)
            // Добавляем команду печати этикетки (1 копию)
            context.Buffer.AddRange(context.Encoding.GetBytes("P1\n"));
        yield return context.Buffer;
    }

    public override IEnumerable<IList<byte>> GetTestDocumentAsByteStream(int testKind)
    {
        var context = new PrintingLayoutContext();

        // Очищаем графический буфер принтера 
        context.Buffer.AddRange(context.Encoding.GetBytes("\nN\n"));

        if (printOrientation == PrintOrientation.TOP_TO_BOTTOM)
            // Устанавливаем направление печати (сверху вниз)
            context.Buffer.AddRange(context.Encoding.GetBytes("ZT\n"));
        else if (printOrientation == PrintOrientation.BOTTOM_TO_TOP)
            // Устанавливаем направление печати (снизу вверх)
            context.Buffer.AddRange(context.Encoding.GetBytes("ZB\n"));

        //Устанавливаем размер этикетки
        context.Buffer.AddRange(context.Encoding.GetBytes(string.Format(CultureInfo.InvariantCulture, "q{0}\nQ{1},26\n",
            PageSettings.PrintableWidth, PageSettings.PrintableHeight)));

        WriteRectangle(context, 0, 0, PageSettings.PrintableWidthOrDefault - 1, PageSettings.PrintableHeightOrDefault - 1);

        context.Buffer.AddRange(context.Encoding.GetBytes("P1\n"));
        yield return context.Buffer;
    }

    #endregion

    #region Helper methods

    private static void WriteImage([NotNull] IPrintingLayoutContext context, [CanBeNull] MonoImage image,
        [NotNull] IPrinterPageSettings pageSettings, string align)
    {
        if (image == null)
            return;

        var posX = PrinterDriverHelper.GetPositionX(pageSettings, image.Width, align, false);
        var (imageData, bytesPerRow) = ImageHelper.ImageToByteArray(image, true);

        var cmd = string.Format(CultureInfo.InvariantCulture, "GW{0},{1},{2},{3},", posX, context.PosY, bytesPerRow, image.Height);
        context.Buffer.AddRange(context.Encoding.GetBytes(cmd));
        context.Buffer.AddRange(imageData);
        context.Buffer.AddRange(context.Encoding.GetBytes("\n"));
    }
    public void WriteRectangle([NotNull] IPrintingLayoutContext context, int x1, int y1, int x2, int y2)
    {
        const int textMargin = 5;
        var esc = fontsEsc["f0"].esc;
        var vm = fontsEsc["f0"].vertMultiply;

        //Координаты верхнего левого угла
        var text = $"({x1},{y1})";
        var textMetrics = GetTextSize(text);
        context.Buffer.AddRange(context.Encoding.GetBytes($"A{textMargin},{textMetrics.Height + textMargin},0,{esc},1,{vm},N,\"{text}\"\n"));
        
        //Координаты верхнего правого угла
        text = $"({x2},{y1})";
        textMetrics = GetTextSize(text);
        context.Buffer.AddRange(context.Encoding.GetBytes($"A{x2 - textMetrics.Width - textMargin},{textMetrics.Height + textMargin},0,{esc},1,{vm},N,\"{text}\"\n"));

        //Координаты нижнего левого угла
        text = $"({x1},{y2})";
        textMetrics = GetTextSize(text);
        context.Buffer.AddRange(context.Encoding.GetBytes($"A{textMargin},{y2 - textMetrics.Height - textMargin},0,{esc},1,{vm},N,\"{text}\"\n"));

        //Координаты нижнего правого угла
        text = $"({x2},{y2})";
        textMetrics = GetTextSize(text);
        context.Buffer.AddRange(context.Encoding.GetBytes($"A{x2 - textMetrics.Width - textMargin},{y2 - textMetrics.Height - textMargin},0,{esc},1,{vm},N,\"{text}\"\n"));

        //Прямоугольник толщиной 1
        context.Buffer.AddRange(context.Encoding.GetBytes($"X{x1},{y1},1,{x2},{y2}\n"));
    }

    public ElementMetrics GetTextSize(string text)
    {
        var textWidth = (fontSizes["f0"].Width + fontSizes["f0"].MarginRight) * text.Length;
        var textheight = fontSizes["f0"].Height;
        return new ElementMetrics(textWidth, textheight);
    }

    #endregion
}