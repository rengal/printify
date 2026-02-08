using System.Text;
using EscPosCommands = Printify.Infrastructure.Printing.EscPos.Commands;
using Printify.Domain.Media;
using Printify.Domain.Printing;
using Printify.Domain.Specifications;
using Printify.Web.Contracts.Documents.Responses.Canvas.Elements;
using Xunit;

namespace Printify.Tests.Shared.EscPos;

/// <summary>
/// Provides shared ESC/POS golden cases and their expected documents.
/// </summary>
public static class EscPosGoldenCases
{
    private static readonly bool EncodingProviderRegistered = RegisterEncodingProvider();
    private static readonly
        IReadOnlyDictionary<string, (IReadOnlyList<Command>, IReadOnlyList<Command>?, IReadOnlyList<CanvasElementDto>)>
            expectations =
                new Dictionary<string, (
                    IReadOnlyList<Command> expectedRequestElement,
                    IReadOnlyList<Command>? expectedFinalizedElements,
                    IReadOnlyList<CanvasElementDto> expectedCanvasElements)>
            {
                ["case01"] = (
                    expectedRequestElement:
                    [
                        new EscPosCommands.EscPosInitialize { LengthInBytes = 2 },
                        new EscPosCommands.EscPosSelectFont(0, false, false) { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSetCodePage("866") { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSelectFont(0, false, false) { LengthInBytes = 3 },
                        CreateAppendText(Pad("font 0", 42)),
                        new EscPosCommands.EscPosPrintAndLineFeed { LengthInBytes = 1 },
                        new EscPosCommands.EscPosCutPaper(EscPosCommands.EscPosPagecutMode.Partial, 0) { LengthInBytes = 4 }
                    ],
                    expectedFinalizedElements: null, // the same as elements
                    expectedCanvasElements:
                    [
                        new CanvasDebugElementDto("resetPrinter") { LengthInBytes = 2 },
                        new CanvasDebugElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                        new CanvasDebugElementDto("setCodePage", CodePageParameters("866")) { LengthInBytes = 3 },
                        new CanvasDebugElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                        new CanvasDebugElementDto("appendToLineBuffer", AppendTextParameters(Pad("font 0", 42)))
                        { LengthInBytes = 42 },
                        new CanvasDebugElementDto("flushLineBufferAndFeed") { LengthInBytes = 1 },
                        new CanvasTextElementDto(
                            Pad("font 0", 42),
                            0,
                            0,
                            504,
                            24,
                            EscPosSpecs.Fonts.FontA.FontName,
                            0,
                            false,
                            false,
                            false) { LengthInBytes = 42 },
                        new CanvasDebugElementDto("pagecut", PagecutParameters(EscPosCommands.EscPosPagecutMode.Partial, 0)) { LengthInBytes = 4 }
                    ]),
                ["case02"] = (
                    expectedRequestElement:
                    [
                        new EscPosCommands.EscPosInitialize { LengthInBytes = 2 },
                        new EscPosCommands.EscPosSelectFont(0, false, false) { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSetCodePage("866") { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSelectFont(0, false, false) { LengthInBytes = 3 },
                        CreateAppendText(Pad("font 0", 42)),
                        new EscPosCommands.EscPosPrintAndLineFeed { LengthInBytes = 1 },
                        new EscPosCommands.EscPosSelectFont(1, true, true) { LengthInBytes = 3 },
                        CreateAppendText(Pad("font 1", 28)),
                        new EscPosCommands.EscPosPrintAndLineFeed { LengthInBytes = 1 },
                        new EscPosCommands.EscPosSelectFont(0, true, true) { LengthInBytes = 3 },
                        CreateAppendText(Pad("font 2", 21)),
                        new EscPosCommands.EscPosPrintAndLineFeed { LengthInBytes = 1 },
                        new EscPosCommands.EscPosCutPaper(EscPosCommands.EscPosPagecutMode.Partial, 0) { LengthInBytes = 4 }
                    ],
                    expectedFinalizedElements: null, // the same as elements
                    expectedCanvasElements:
                    [
                        new CanvasDebugElementDto("resetPrinter") { LengthInBytes = 2 },
                        new CanvasDebugElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                        new CanvasDebugElementDto("setCodePage", CodePageParameters("866")) { LengthInBytes = 3 },
                        new CanvasDebugElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                        new CanvasDebugElementDto("appendToLineBuffer", AppendTextParameters(Pad("font 0", 42)))
                        { LengthInBytes = 42 },
                        new CanvasDebugElementDto("flushLineBufferAndFeed") { LengthInBytes = 1 },
                        new CanvasTextElementDto(
                            Pad("font 0", 42),
                            0,
                            0,
                            504,
                            24,
                            EscPosSpecs.Fonts.FontA.FontName,
                            0,
                            false,
                            false,
                            false) { LengthInBytes = 42 },
                        new CanvasDebugElementDto("setFont", SetFontParameters(1, true, true)) { LengthInBytes = 3 },
                        new CanvasDebugElementDto("appendToLineBuffer", AppendTextParameters(Pad("font 1", 28)))
                        { LengthInBytes = 28 },
                        new CanvasDebugElementDto("flushLineBufferAndFeed") { LengthInBytes = 1 },
                        new CanvasTextElementDto(
                            Pad("font 1", 28),
                            0,
                            24,
                            672,
                            48,
                            EscPosSpecs.Fonts.FontB.FontName,
                            0,
                            false,
                            false,
                            false,
                            CharScaleX: 2,
                            CharScaleY: 2)
                        { LengthInBytes = 28 },
                        new CanvasDebugElementDto("setFont", SetFontParameters(0, true, true)) { LengthInBytes = 3 },
                        new CanvasDebugElementDto("appendToLineBuffer", AppendTextParameters(Pad("font 2", 21)))
                        { LengthInBytes = 21 },
                        new CanvasDebugElementDto("flushLineBufferAndFeed") { LengthInBytes = 1 },
                        new CanvasTextElementDto(
                            Pad("font 2", 21),
                            0,
                            72,
                            504,
                            48,
                            EscPosSpecs.Fonts.FontA.FontName,
                            0,
                            false,
                            false,
                            false,
                            CharScaleX: 2,
                            CharScaleY: 2)
                        { LengthInBytes = 21 },
                        new CanvasDebugElementDto("pagecut", PagecutParameters(EscPosCommands.EscPosPagecutMode.Partial, 0)) { LengthInBytes = 4 }
                    ]),
                ["case03"] = (
                    expectedRequestElement:
                    [
                        new EscPosCommands.EscPosInitialize { LengthInBytes = 2 },
                        new EscPosCommands.EscPosSelectFont(0, false, false) { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSetCodePage("866") { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSelectFont(0, false, false) { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSetJustification(EscPosCommands.EscPosTextJustification.Right) { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSetBarcodeHeight(101) { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSetBarcodeModuleWidth(3) { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSetBarcodeLabelPosition(EscPosCommands.EscPosBarcodeLabelPosition.Below) { LengthInBytes = 3 },
                        new EscPosCommands.EscPosPrintBarcodeUpload(EscPosCommands.EscPosBarcodeSymbology.Ean13, "1234567890128") { LengthInBytes = 17 },
                        new EscPosCommands.EscPosSetJustification(EscPosCommands.EscPosTextJustification.Left) { LengthInBytes = 3 },
                        new EscPosCommands.EscPosPrintAndLineFeed { LengthInBytes = 1 },
                        new EscPosCommands.EscPosCutPaper(EscPosCommands.EscPosPagecutMode.Partial, 0) { LengthInBytes = 4 }
                    ],
                    expectedFinalizedElements:
                    [
                        new EscPosCommands.EscPosInitialize { LengthInBytes = 2 },
                        new EscPosCommands.EscPosSelectFont(0, false, false) { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSetCodePage("866") { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSelectFont(0, false, false) { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSetJustification(EscPosCommands.EscPosTextJustification.Right) { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSetBarcodeHeight(101) { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSetBarcodeModuleWidth(3) { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSetBarcodeLabelPosition(EscPosCommands.EscPosBarcodeLabelPosition.Below) { LengthInBytes = 3 },
                        new EscPosCommands.EscPosPrintBarcode(EscPosCommands.EscPosBarcodeSymbology.Ean13, "1234567890128", 0, 0, Media.CreateDefaultPng(1)) { LengthInBytes = 17 },
                        new EscPosCommands.EscPosSetJustification(EscPosCommands.EscPosTextJustification.Left) { LengthInBytes = 3 },
                        new EscPosCommands.EscPosPrintAndLineFeed { LengthInBytes = 1 },
                        new EscPosCommands.EscPosCutPaper(EscPosCommands.EscPosPagecutMode.Partial, 0) { LengthInBytes = 4 }
                    ],
                    expectedCanvasElements:
                    [
                        new CanvasDebugElementDto("resetPrinter") { LengthInBytes = 2 },
                        new CanvasDebugElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                        new CanvasDebugElementDto("setCodePage", CodePageParameters("866")) { LengthInBytes = 3 },
                        new CanvasDebugElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                        new CanvasDebugElementDto(
                            "setJustification",
                            JustificationParameters(EscPosCommands.EscPosTextJustification.Right)) { LengthInBytes = 3 },
                        new CanvasDebugElementDto("setBarcodeHeight", BarcodeHeightParameters(101)) { LengthInBytes = 3 },
                        new CanvasDebugElementDto("setBarcodeModuleWidth", BarcodeModuleWidthParameters(3)) { LengthInBytes = 3 },
                        new CanvasDebugElementDto(
                            "setBarcodeLabelPosition",
                            BarcodeLabelParameters(EscPosCommands.EscPosBarcodeLabelPosition.Below)) { LengthInBytes = 3 },
                        new CanvasDebugElementDto("printBarcode") { LengthInBytes = 17 },
                        new CanvasImageElementDto(
                            ToViewMediaDto(Media.CreateDefaultPng(1)),
                            0,
                            0,
                            512,
                            101) { LengthInBytes = 17 },
                        new CanvasDebugElementDto(
                            "setJustification",
                            JustificationParameters(EscPosCommands.EscPosTextJustification.Left)) { LengthInBytes = 3 },
                        new CanvasDebugElementDto("flushLineBufferAndFeed") { LengthInBytes = 1 },
                        new CanvasDebugElementDto("pagecut", PagecutParameters(EscPosCommands.EscPosPagecutMode.Partial, 0)) { LengthInBytes = 4 }
                    ]),
                ["case04"] = (
                    expectedRequestElement:
                    [
                        new EscPosCommands.EscPosInitialize { LengthInBytes = 2 },
                        new EscPosCommands.EscPosSelectFont(0, false, false) { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSetCodePage("866") { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSetJustification(EscPosCommands.EscPosTextJustification.Left) { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSetQrModel(EscPosCommands.EscPosQrModel.Model2) { LengthInBytes = 9 },
                        new EscPosCommands.EscPosSetQrModuleSize(7) { LengthInBytes = 8 },
                        new EscPosCommands.EscPosSetQrErrorCorrection(EscPosCommands.EscPosQrErrorCorrectionLevel.Low) { LengthInBytes = 8 },
                        new EscPosCommands.EscPosStoreQrData("https://google.com") { LengthInBytes = 26 },
                        new EscPosCommands.EscPosPrintQrCodeUpload { LengthInBytes = 8 },
                        new EscPosCommands.EscPosPrintAndLineFeed { LengthInBytes = 1 },
                        new EscPosCommands.EscPosCutPaper(EscPosCommands.EscPosPagecutMode.Partial, 0) { LengthInBytes = 4 }
                    ],
                    expectedFinalizedElements:
                    [
                        new EscPosCommands.EscPosInitialize { LengthInBytes = 2 },
                        new EscPosCommands.EscPosSelectFont(0, false, false) { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSetCodePage("866") { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSetJustification(EscPosCommands.EscPosTextJustification.Left) { LengthInBytes = 3 },
                        new EscPosCommands.EscPosSetQrModel(EscPosCommands.EscPosQrModel.Model2) { LengthInBytes = 9 },
                        new EscPosCommands.EscPosSetQrModuleSize(7) { LengthInBytes = 8 },
                        new EscPosCommands.EscPosSetQrErrorCorrection(EscPosCommands.EscPosQrErrorCorrectionLevel.Low) { LengthInBytes = 8 },
                        new EscPosCommands.EscPosStoreQrData("https://google.com") { LengthInBytes = 26 },
                        new EscPosCommands.EscPosPrintQrCode("https://google.com", 0, 0, Media.CreateDefaultPng(2)) { LengthInBytes = 8 },
                        new EscPosCommands.EscPosPrintAndLineFeed { LengthInBytes = 1 },
                        new EscPosCommands.EscPosCutPaper(EscPosCommands.EscPosPagecutMode.Partial, 0) { LengthInBytes = 4 }
                    ],
                    expectedCanvasElements:
                    [
                        new CanvasDebugElementDto("resetPrinter") { LengthInBytes = 2 },
                        new CanvasDebugElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                        new CanvasDebugElementDto("setCodePage", CodePageParameters("866")) { LengthInBytes = 3 },
                        new CanvasDebugElementDto(
                            "setJustification",
                            JustificationParameters(EscPosCommands.EscPosTextJustification.Left)) { LengthInBytes = 3 },
                        new CanvasDebugElementDto("setQrModel", QrModelParameters(EscPosCommands.EscPosQrModel.Model2)) { LengthInBytes = 9 },
                        new CanvasDebugElementDto("setQrModuleSize", QrModuleSizeParameters(7)) { LengthInBytes = 8 },
                        new CanvasDebugElementDto(
                            "setQrErrorCorrection",
                            QrErrorCorrectionParameters(EscPosCommands.EscPosQrErrorCorrectionLevel.Low)) { LengthInBytes = 8 },
                        new CanvasDebugElementDto(
                            "storeQrData",
                            StoreQrDataParameters("https://google.com")) { LengthInBytes = 26 },
                        new CanvasDebugElementDto("printQrCode") { LengthInBytes = 8 },
                        new CanvasImageElementDto(
                            ToViewMediaDto(Media.CreateDefaultPng(2)),
                            0,
                            0,
                            512,
                            225) { LengthInBytes = 8 },
                        new CanvasDebugElementDto("flushLineBufferAndFeed") { LengthInBytes = 1 },
                        new CanvasDebugElementDto("pagecut", PagecutParameters(EscPosCommands.EscPosPagecutMode.Partial, 0)) { LengthInBytes = 4 }
                    ]),
                ["case05"] = (
                    expectedRequestElement:
                        [
                            new EscPosCommands.EscPosInitialize { LengthInBytes = 2 },
                            new EscPosCommands.EscPosSelectFont(0, false, false) { LengthInBytes = 3 },
                            new EscPosCommands.EscPosPrintLogo(0) { LengthInBytes = 4 },
                            new EscPosCommands.EscPosCutPaper(EscPosCommands.EscPosPagecutMode.Partial, 0) { LengthInBytes = 4 }
                        ],
                        expectedFinalizedElements: null, // the same as elements
                        expectedCanvasElements:
                        [
                            new CanvasDebugElementDto("resetPrinter") { LengthInBytes = 2 },
                            new CanvasDebugElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                            new CanvasDebugElementDto("storedLogo", StoredLogoParameters(0)) { LengthInBytes = 4 },
                            new CanvasDebugElementDto("pagecut", PagecutParameters(EscPosCommands.EscPosPagecutMode.Partial, 0)) { LengthInBytes = 4 }
                        ]),
            };

    public static
        IReadOnlyDictionary<string, (
            IReadOnlyList<Command> expectedRequestElement,
            IReadOnlyList<Command>? expectedPersistedElements,
            IReadOnlyList<CanvasElementDto> expectedViewElements)> Expectations => expectations;

    public static TheoryData<string, byte[]> Cases { get; } = BuildCases();

    private static TheoryData<string, byte[]> BuildCases()
    {
        var data = new TheoryData<string, byte[]>();
        var assembly = typeof(EscPosGoldenCases).Assembly;

        var resources = assembly
            .GetManifestResourceNames()
            .Where(name => name.EndsWith(".b64", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);

        foreach (var resourceName in resources)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                continue;
            }

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var base64 = reader.ReadToEnd();
            if (string.IsNullOrWhiteSpace(base64))
            {
                continue;
            }

            try
            {
                var bytes = Convert.FromBase64String(base64);
                var caseId = GetCaseId(resourceName);
                data.Add(caseId, bytes);
            }
            catch (FormatException)
            {
                // Ignore malformed placeholders; content will be filled later.
            }
        }

        return data;
    }

    private static string GetCaseId(string resourceName)
    {
        var withoutExtension = resourceName[..resourceName.LastIndexOf(".", StringComparison.Ordinal)];
        var lastSeparator = withoutExtension.LastIndexOf(".", StringComparison.Ordinal);
        return lastSeparator >= 0
            ? withoutExtension[(lastSeparator + 1)..]
            : withoutExtension;
    }

    private static string Pad(string text, int totalLength) => text.PadRight(totalLength);

    private static CanvasMediaDto ToViewMediaDto(Media media)
    {
        return new CanvasMediaDto(
            media.ContentType,
            ToMediaSize(media.Length),
            media.Url,
            media.FileName);
    }

    private static int ToMediaSize(long length)
    {
        return length > int.MaxValue ? int.MaxValue : (int)length;
    }

    private static IReadOnlyDictionary<string, string> SetFontParameters(
        int fontNumber,
        bool isDoubleWidth,
        bool isDoubleHeight)
    {
        return new Dictionary<string, string>
        {
            ["FontNumber"] = fontNumber.ToString(),
            ["IsDoubleWidth"] = isDoubleWidth.ToString(),
            ["IsDoubleHeight"] = isDoubleHeight.ToString()
        };
    }

    private static IReadOnlyDictionary<string, string> CodePageParameters(string codePage)
    {
        return new Dictionary<string, string>
        {
            ["CodePage"] = codePage
        };
    }

    private static IReadOnlyDictionary<string, string> JustificationParameters(EscPosCommands.EscPosTextJustification justification)
    {
        return new Dictionary<string, string>
        {
            ["Justification"] = justification.ToString()
        };
    }

    private static IReadOnlyDictionary<string, string> BarcodeHeightParameters(int heightInDots)
    {
        return new Dictionary<string, string>
        {
            ["HeightInDots"] = heightInDots.ToString()
        };
    }

    private static IReadOnlyDictionary<string, string> BarcodeModuleWidthParameters(int moduleWidth)
    {
        return new Dictionary<string, string>
        {
            ["ModuleWidth"] = moduleWidth.ToString()
        };
    }

    private static IReadOnlyDictionary<string, string> BarcodeLabelParameters(EscPosCommands.EscPosBarcodeLabelPosition position)
    {
        return new Dictionary<string, string>
        {
            ["Position"] = position.ToString()
        };
    }

    private static IReadOnlyDictionary<string, string> QrModelParameters(EscPosCommands.EscPosQrModel model)
    {
        return new Dictionary<string, string>
        {
            ["Model"] = model.ToString()
        };
    }

    private static IReadOnlyDictionary<string, string> QrModuleSizeParameters(int moduleSize)
    {
        return new Dictionary<string, string>
        {
            ["ModuleSize"] = moduleSize.ToString()
        };
    }

    private static IReadOnlyDictionary<string, string> QrErrorCorrectionParameters(EscPosCommands.EscPosQrErrorCorrectionLevel level)
    {
        return new Dictionary<string, string>
        {
            ["Level"] = level.ToString()
        };
    }

    private static IReadOnlyDictionary<string, string> StoreQrDataParameters(string content)
    {
        return new Dictionary<string, string>
        {
            ["Content"] = content
        };
    }

    private static IReadOnlyDictionary<string, string> StoredLogoParameters(int logoId)
    {
        return new Dictionary<string, string>
        {
            ["LogoId"] = logoId.ToString()
        };
    }

    private static IReadOnlyDictionary<string, string> PagecutParameters(EscPosCommands.EscPosPagecutMode mode, int? feedUnits)
    {
        return new Dictionary<string, string>
        {
            ["Mode"] = mode.ToString(),
            ["FeedMotionUnits"] = feedUnits?.ToString() ?? string.Empty
        };
    }

    private static IReadOnlyDictionary<string, string> AppendTextParameters(string text)
    {
        return new Dictionary<string, string>
        {
            ["Text"] = text
        };
    }

    private static EscPosCommands.EscPosAppendText CreateAppendText(string text, Encoding? encoding = null)
    {
        encoding ??= Encoding.GetEncoding(437);
        var bytes = encoding.GetBytes(text);
        return new EscPosCommands.EscPosAppendText(bytes) { LengthInBytes = bytes.Length };
    }

    private static bool RegisterEncodingProvider()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return true;
    }
}
