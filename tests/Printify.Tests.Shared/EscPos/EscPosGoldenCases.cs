using System.Text;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Media;
using Printify.Web.Contracts.Documents.Responses.View.Elements;
using Xunit;

namespace Printify.Tests.Shared.EscPos;

/// <summary>
/// Provides shared ESC/POS golden cases and their expected documents.
/// </summary>
public static class EscPosGoldenCases
{
    private static readonly
        IReadOnlyDictionary<string, (IReadOnlyList<Element>, IReadOnlyList<Element>?, IReadOnlyList<ViewElementDto>)>
            expectations =
                new Dictionary<string, (
                    IReadOnlyList<Element> expectedRequestElement,
                    IReadOnlyList<Element>? expectedFinalizedElements,
                    IReadOnlyList<ViewElementDto> expectedViewElements)>
            {
                ["case01"] = (
                    expectedRequestElement:
                    [
                        new ResetPrinter { LengthInBytes = 2 },
                        new SetFont(0, false, false) { LengthInBytes = 3 },
                        new SetCodePage("866") { LengthInBytes = 3 },
                        new SetFont(0, false, false) { LengthInBytes = 3 },
                        new AppendToLineBuffer(Pad("font 0", 42)) { LengthInBytes = 42 },
                        new FlushLineBufferAndFeed { LengthInBytes = 1 },
                        new Pagecut(PagecutMode.Partial, 0) { LengthInBytes = 4 }
                    ],
                    expectedFinalizedElements: null, // the same as elements
                    expectedViewElements:
                    [
                        new ViewStateElementDto("resetPrinter") { LengthInBytes = 2 },
                        new ViewStateElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                        new ViewStateElementDto("setCodePage", CodePageParameters("866")) { LengthInBytes = 3 },
                        new ViewStateElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                        new ViewStateElementDto("flushLineBufferAndFeed") { LengthInBytes = 1 },
                        new ViewTextElementDto(
                            Pad("font 0", 42),
                            0,
                            0,
                            504,
                            24,
                            ViewFontNames.EscPosA,
                            0,
                            false,
                            false,
                            false) { LengthInBytes = 42 },
                        new ViewStateElementDto("pagecut", PagecutParameters(PagecutMode.Partial, 0)) { LengthInBytes = 4 }
                    ]),
                ["case02"] = (
                    expectedRequestElement:
                    [
                        new ResetPrinter { LengthInBytes = 2 },
                        new SetFont(0, false, false) { LengthInBytes = 3 },
                        new SetCodePage("866") { LengthInBytes = 3 },
                        new SetFont(0, false, false) { LengthInBytes = 3 },
                        new AppendToLineBuffer(Pad("font 0", 42)) { LengthInBytes = 42 },
                        new FlushLineBufferAndFeed { LengthInBytes = 1 },
                        new SetFont(1, true, true) { LengthInBytes = 3 },
                        new AppendToLineBuffer(Pad("font 1", 28)) { LengthInBytes = 28 },
                        new FlushLineBufferAndFeed { LengthInBytes = 1 },
                        new SetFont(0, true, true) { LengthInBytes = 3 },
                        new AppendToLineBuffer(Pad("font 2", 21)) { LengthInBytes = 21 },
                        new FlushLineBufferAndFeed { LengthInBytes = 1 },
                        new Pagecut(PagecutMode.Partial, 0) { LengthInBytes = 4 }
                    ],
                    expectedFinalizedElements: null, // the same as elements
                    expectedViewElements:
                    [
                        new ViewStateElementDto("resetPrinter") { LengthInBytes = 2 },
                        new ViewStateElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                        new ViewStateElementDto("setCodePage", CodePageParameters("866")) { LengthInBytes = 3 },
                        new ViewStateElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                        new ViewStateElementDto("flushLineBufferAndFeed") { LengthInBytes = 1 },
                        new ViewTextElementDto(
                            Pad("font 0", 42),
                            0,
                            0,
                            504,
                            24,
                            ViewFontNames.EscPosA,
                            0,
                            false,
                            false,
                            false) { LengthInBytes = 42 },
                        new ViewStateElementDto("setFont", SetFontParameters(1, true, true)) { LengthInBytes = 3 },
                        new ViewStateElementDto("flushLineBufferAndFeed") { LengthInBytes = 1 },
                        new ViewTextElementDto(
                            Pad("font 1", 28),
                            0,
                            28,
                            504,
                            34,
                            ViewFontNames.EscPosB,
                            0,
                            false,
                            false,
                            false)
                        { CharScaleX = 2, CharScaleY = 2, LengthInBytes = 28 },
                        new ViewStateElementDto("setFont", SetFontParameters(0, true, true)) { LengthInBytes = 3 },
                        new ViewStateElementDto("flushLineBufferAndFeed") { LengthInBytes = 1 },
                        new ViewTextElementDto(
                            Pad("font 2", 21),
                            0,
                            66,
                            504,
                            48,
                            ViewFontNames.EscPosA,
                            0,
                            false,
                            false,
                            false)
                        { CharScaleX = 2, CharScaleY = 2, LengthInBytes = 21 },
                        new ViewStateElementDto("pagecut", PagecutParameters(PagecutMode.Partial, 0)) { LengthInBytes = 4 }
                    ]),
                ["case03"] = (
                    expectedRequestElement:
                    [
                        new ResetPrinter { LengthInBytes = 2 },
                        new SetFont(0, false, false) { LengthInBytes = 3 },
                        new SetCodePage("866") { LengthInBytes = 3 },
                        new SetFont(0, false, false) { LengthInBytes = 3 },
                        new SetJustification(TextJustification.Right) { LengthInBytes = 3 },
                        new SetBarcodeHeight(101) { LengthInBytes = 3 },
                        new SetBarcodeModuleWidth(3) { LengthInBytes = 3 },
                        new SetBarcodeLabelPosition(BarcodeLabelPosition.Below) { LengthInBytes = 3 },
                        new PrintBarcodeUpload(BarcodeSymbology.Ean13, "1234567890128") { LengthInBytes = 17 },
                        new SetJustification(TextJustification.Left) { LengthInBytes = 3 },
                        new FlushLineBufferAndFeed { LengthInBytes = 1 },
                        new Pagecut(PagecutMode.Partial, 0) { LengthInBytes = 4 }
                    ],
                    expectedFinalizedElements:
                    [
                        new ResetPrinter { LengthInBytes = 2 },
                        new SetFont(0, false, false) { LengthInBytes = 3 },
                        new SetCodePage("866") { LengthInBytes = 3 },
                        new SetFont(0, false, false) { LengthInBytes = 3 },
                        new SetJustification(TextJustification.Right) { LengthInBytes = 3 },
                        new SetBarcodeHeight(101) { LengthInBytes = 3 },
                        new SetBarcodeModuleWidth(3) { LengthInBytes = 3 },
                        new SetBarcodeLabelPosition(BarcodeLabelPosition.Below) { LengthInBytes = 3 },
                        new PrintBarcode(BarcodeSymbology.Ean13, "1234567890128", 0, 0, Media.CreateDefaultPng(1)) { LengthInBytes = 17 },
                        new SetJustification(TextJustification.Left) { LengthInBytes = 3 },
                        new FlushLineBufferAndFeed { LengthInBytes = 1 },
                        new Pagecut(PagecutMode.Partial, 0) { LengthInBytes = 4 }
                    ],
                    expectedViewElements:
                    [
                        new ViewStateElementDto("resetPrinter") { LengthInBytes = 2 },
                        new ViewStateElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                        new ViewStateElementDto("setCodePage", CodePageParameters("866")) { LengthInBytes = 3 },
                        new ViewStateElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                        new ViewStateElementDto(
                            "setJustification",
                            JustificationParameters(TextJustification.Right)) { LengthInBytes = 3 },
                        new ViewStateElementDto("setBarcodeHeight", BarcodeHeightParameters(101)) { LengthInBytes = 3 },
                        new ViewStateElementDto("setBarcodeModuleWidth", BarcodeModuleWidthParameters(3)) { LengthInBytes = 3 },
                        new ViewStateElementDto(
                            "setBarcodeLabelPosition",
                            BarcodeLabelParameters(BarcodeLabelPosition.Below)) { LengthInBytes = 3 },
                        new ViewImageElementDto(
                            ToViewMediaDto(Media.CreateDefaultPng(1)),
                            0,
                            0,
                            512,
                            101) { LengthInBytes = 17 },
                        new ViewStateElementDto(
                            "setJustification",
                            JustificationParameters(TextJustification.Left)) { LengthInBytes = 3 },
                        new ViewStateElementDto("flushLineBufferAndFeed") { LengthInBytes = 1 },
                        new ViewStateElementDto("pagecut", PagecutParameters(PagecutMode.Partial, 0)) { LengthInBytes = 4 }
                    ]),
                ["case04"] = (
                    expectedRequestElement:
                    [
                        new ResetPrinter { LengthInBytes = 2 },
                        new SetFont(0, false, false) { LengthInBytes = 3 },
                        new SetCodePage("866") { LengthInBytes = 3 },
                        new SetJustification(TextJustification.Left) { LengthInBytes = 3 },
                        new SetQrModel(QrModel.Model2) { LengthInBytes = 9 },
                        new SetQrModuleSize(7) { LengthInBytes = 8 },
                        new SetQrErrorCorrection(QrErrorCorrectionLevel.Low) { LengthInBytes = 8 },
                        new StoreQrData("https://google.com") { LengthInBytes = 26 },
                        new PrintQrCodeUpload { LengthInBytes = 8 },
                        new FlushLineBufferAndFeed { LengthInBytes = 1 },
                        new Pagecut(PagecutMode.Partial, 0) { LengthInBytes = 4 }
                    ],
                    expectedFinalizedElements:
                    [
                        new ResetPrinter { LengthInBytes = 2 },
                        new SetFont(0, false, false) { LengthInBytes = 3 },
                        new SetCodePage("866") { LengthInBytes = 3 },
                        new SetJustification(TextJustification.Left) { LengthInBytes = 3 },
                        new SetQrModel(QrModel.Model2) { LengthInBytes = 9 },
                        new SetQrModuleSize(7) { LengthInBytes = 8 },
                        new SetQrErrorCorrection(QrErrorCorrectionLevel.Low) { LengthInBytes = 8 },
                        new StoreQrData("https://google.com") { LengthInBytes = 26 },
                        new PrintQrCode("https://google.com", 0, 0, Media.CreateDefaultPng(2)) { LengthInBytes = 8 },
                        new FlushLineBufferAndFeed { LengthInBytes = 1 },
                        new Pagecut(PagecutMode.Partial, 0) { LengthInBytes = 4 }
                    ],
                    expectedViewElements:
                    [
                        new ViewStateElementDto("resetPrinter") { LengthInBytes = 2 },
                        new ViewStateElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                        new ViewStateElementDto("setCodePage", CodePageParameters("866")) { LengthInBytes = 3 },
                        new ViewStateElementDto(
                            "setJustification",
                            JustificationParameters(TextJustification.Left)) { LengthInBytes = 3 },
                        new ViewStateElementDto("setQrModel", QrModelParameters(QrModel.Model2)) { LengthInBytes = 9 },
                        new ViewStateElementDto("setQrModuleSize", QrModuleSizeParameters(7)) { LengthInBytes = 8 },
                        new ViewStateElementDto(
                            "setQrErrorCorrection",
                            QrErrorCorrectionParameters(QrErrorCorrectionLevel.Low)) { LengthInBytes = 8 },
                        new ViewStateElementDto(
                            "storeQrData",
                            StoreQrDataParameters("https://google.com")) { LengthInBytes = 26 },
                        new ViewImageElementDto(
                            ToViewMediaDto(Media.CreateDefaultPng(2)),
                            0,
                            0,
                            512,
                            175) { LengthInBytes = 8 },
                        new ViewStateElementDto("flushLineBufferAndFeed") { LengthInBytes = 1 },
                        new ViewStateElementDto("pagecut", PagecutParameters(PagecutMode.Partial, 0)) { LengthInBytes = 4 }
                    ]),
                ["case05"] = (
                    expectedRequestElement:
                        [
                            new ResetPrinter { LengthInBytes = 2 },
                            new SetFont(0, false, false) { LengthInBytes = 3 },
                            new StoredLogo(0) { LengthInBytes = 4 },
                            new Pagecut(PagecutMode.Partial, 0) { LengthInBytes = 4 }
                        ],
                        expectedFinalizedElements: null, // the same as elements
                        expectedViewElements:
                        [
                            new ViewStateElementDto("resetPrinter") { LengthInBytes = 2 },
                            new ViewStateElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                            new ViewStateElementDto("storedLogo", StoredLogoParameters(0)) { LengthInBytes = 4 },
                            new ViewStateElementDto("pagecut", PagecutParameters(PagecutMode.Partial, 0)) { LengthInBytes = 4 }
                        ]),
            };

    public static
        IReadOnlyDictionary<string, (
            IReadOnlyList<Element> expectedRequestElement,
            IReadOnlyList<Element>? expectedPersistedElements,
            IReadOnlyList<ViewElementDto> expectedViewElements)> Expectations => expectations;

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

    private static ViewMediaDto ToViewMediaDto(Media media)
    {
        return new ViewMediaDto(media.ContentType, media.Length, media.Sha256Checksum, media.Url);
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

    private static IReadOnlyDictionary<string, string> JustificationParameters(TextJustification justification)
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

    private static IReadOnlyDictionary<string, string> BarcodeLabelParameters(BarcodeLabelPosition position)
    {
        return new Dictionary<string, string>
        {
            ["Position"] = position.ToString()
        };
    }

    private static IReadOnlyDictionary<string, string> QrModelParameters(QrModel model)
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

    private static IReadOnlyDictionary<string, string> QrErrorCorrectionParameters(QrErrorCorrectionLevel level)
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

    private static IReadOnlyDictionary<string, string> PagecutParameters(PagecutMode mode, int? feedUnits)
    {
        return new Dictionary<string, string>
        {
            ["Mode"] = mode.ToString(),
            ["FeedMotionUnits"] = feedUnits?.ToString() ?? string.Empty
        };
    }

}
