using System.Text;
using DomainElements = Printify.Domain.Documents.Elements;
using EscPosElements = Printify.Domain.Documents.Elements.EscPos;
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
        IReadOnlyDictionary<string, (IReadOnlyList<DomainElements.Element>, IReadOnlyList<DomainElements.Element>?, IReadOnlyList<ViewElementDto>)>
            expectations =
                new Dictionary<string, (
                    IReadOnlyList<DomainElements.Element> expectedRequestElement,
                    IReadOnlyList<DomainElements.Element>? expectedFinalizedElements,
                    IReadOnlyList<ViewElementDto> expectedViewElements)>
            {
                ["case01"] = (
                    expectedRequestElement:
                    [
                        new EscPosElements.Initialize { LengthInBytes = 2 },
                        new EscPosElements.SelectFont(0, false, false) { LengthInBytes = 3 },
                        new EscPosElements.SetCodePage("866") { LengthInBytes = 3 },
                        new EscPosElements.SelectFont(0, false, false) { LengthInBytes = 3 },
                        new EscPosElements.AppendText(Pad("font 0", 42)) { LengthInBytes = 42 },
                        new EscPosElements.PrintAndLineFeed { LengthInBytes = 1 },
                        new EscPosElements.CutPaper(DomainElements.PagecutMode.Partial, 0) { LengthInBytes = 4 }
                    ],
                    expectedFinalizedElements: null, // the same as elements
                    expectedViewElements:
                    [
                        new ViewDebugElementDto("resetPrinter") { LengthInBytes = 2 },
                        new ViewDebugElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                        new ViewDebugElementDto("setCodePage", CodePageParameters("866")) { LengthInBytes = 3 },
                        new ViewDebugElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                        new ViewDebugElementDto("appendToLineBuffer", AppendTextParameters(Pad("font 0", 42)))
                        { LengthInBytes = 42 },
                        new ViewDebugElementDto("flushLineBufferAndFeed") { LengthInBytes = 1 },
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
                        new ViewDebugElementDto("pagecut", PagecutParameters(DomainElements.PagecutMode.Partial, 0)) { LengthInBytes = 4 }
                    ]),
                ["case02"] = (
                    expectedRequestElement:
                    [
                        new EscPosElements.Initialize { LengthInBytes = 2 },
                        new EscPosElements.SelectFont(0, false, false) { LengthInBytes = 3 },
                        new EscPosElements.SetCodePage("866") { LengthInBytes = 3 },
                        new EscPosElements.SelectFont(0, false, false) { LengthInBytes = 3 },
                        new EscPosElements.AppendText(Pad("font 0", 42)) { LengthInBytes = 42 },
                        new EscPosElements.PrintAndLineFeed { LengthInBytes = 1 },
                        new EscPosElements.SelectFont(1, true, true) { LengthInBytes = 3 },
                        new EscPosElements.AppendText(Pad("font 1", 28)) { LengthInBytes = 28 },
                        new EscPosElements.PrintAndLineFeed { LengthInBytes = 1 },
                        new EscPosElements.SelectFont(0, true, true) { LengthInBytes = 3 },
                        new EscPosElements.AppendText(Pad("font 2", 21)) { LengthInBytes = 21 },
                        new EscPosElements.PrintAndLineFeed { LengthInBytes = 1 },
                        new EscPosElements.CutPaper(DomainElements.PagecutMode.Partial, 0) { LengthInBytes = 4 }
                    ],
                    expectedFinalizedElements: null, // the same as elements
                    expectedViewElements:
                    [
                        new ViewDebugElementDto("resetPrinter") { LengthInBytes = 2 },
                        new ViewDebugElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                        new ViewDebugElementDto("setCodePage", CodePageParameters("866")) { LengthInBytes = 3 },
                        new ViewDebugElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                        new ViewDebugElementDto("appendToLineBuffer", AppendTextParameters(Pad("font 0", 42)))
                        { LengthInBytes = 42 },
                        new ViewDebugElementDto("flushLineBufferAndFeed") { LengthInBytes = 1 },
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
                        new ViewDebugElementDto("setFont", SetFontParameters(1, true, true)) { LengthInBytes = 3 },
                        new ViewDebugElementDto("appendToLineBuffer", AppendTextParameters(Pad("font 1", 28)))
                        { LengthInBytes = 28 },
                        new ViewDebugElementDto("flushLineBufferAndFeed") { LengthInBytes = 1 },
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
                        new ViewDebugElementDto("setFont", SetFontParameters(0, true, true)) { LengthInBytes = 3 },
                        new ViewDebugElementDto("appendToLineBuffer", AppendTextParameters(Pad("font 2", 21)))
                        { LengthInBytes = 21 },
                        new ViewDebugElementDto("flushLineBufferAndFeed") { LengthInBytes = 1 },
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
                        new ViewDebugElementDto("pagecut", PagecutParameters(DomainElements.PagecutMode.Partial, 0)) { LengthInBytes = 4 }
                    ]),
                ["case03"] = (
                    expectedRequestElement:
                    [
                        new EscPosElements.Initialize { LengthInBytes = 2 },
                        new EscPosElements.SelectFont(0, false, false) { LengthInBytes = 3 },
                        new EscPosElements.SetCodePage("866") { LengthInBytes = 3 },
                        new EscPosElements.SelectFont(0, false, false) { LengthInBytes = 3 },
                        new EscPosElements.SetJustification(DomainElements.TextJustification.Right) { LengthInBytes = 3 },
                        new EscPosElements.SetBarcodeHeight(101) { LengthInBytes = 3 },
                        new EscPosElements.SetBarcodeModuleWidth(3) { LengthInBytes = 3 },
                        new EscPosElements.SetBarcodeLabelPosition(DomainElements.BarcodeLabelPosition.Below) { LengthInBytes = 3 },
                        new EscPosElements.PrintBarcodeUpload(DomainElements.BarcodeSymbology.Ean13, "1234567890128") { LengthInBytes = 17 },
                        new EscPosElements.SetJustification(DomainElements.TextJustification.Left) { LengthInBytes = 3 },
                        new EscPosElements.PrintAndLineFeed { LengthInBytes = 1 },
                        new EscPosElements.CutPaper(DomainElements.PagecutMode.Partial, 0) { LengthInBytes = 4 }
                    ],
                    expectedFinalizedElements:
                    [
                        new EscPosElements.Initialize { LengthInBytes = 2 },
                        new EscPosElements.SelectFont(0, false, false) { LengthInBytes = 3 },
                        new EscPosElements.SetCodePage("866") { LengthInBytes = 3 },
                        new EscPosElements.SelectFont(0, false, false) { LengthInBytes = 3 },
                        new EscPosElements.SetJustification(DomainElements.TextJustification.Right) { LengthInBytes = 3 },
                        new EscPosElements.SetBarcodeHeight(101) { LengthInBytes = 3 },
                        new EscPosElements.SetBarcodeModuleWidth(3) { LengthInBytes = 3 },
                        new EscPosElements.SetBarcodeLabelPosition(DomainElements.BarcodeLabelPosition.Below) { LengthInBytes = 3 },
                        new EscPosElements.PrintBarcode(DomainElements.BarcodeSymbology.Ean13, "1234567890128", 0, 0, Media.CreateDefaultPng(1)) { LengthInBytes = 17 },
                        new EscPosElements.SetJustification(DomainElements.TextJustification.Left) { LengthInBytes = 3 },
                        new EscPosElements.PrintAndLineFeed { LengthInBytes = 1 },
                        new EscPosElements.CutPaper(DomainElements.PagecutMode.Partial, 0) { LengthInBytes = 4 }
                    ],
                    expectedViewElements:
                    [
                        new ViewDebugElementDto("resetPrinter") { LengthInBytes = 2 },
                        new ViewDebugElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                        new ViewDebugElementDto("setCodePage", CodePageParameters("866")) { LengthInBytes = 3 },
                        new ViewDebugElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                        new ViewDebugElementDto(
                            "setJustification",
                            JustificationParameters(DomainElements.TextJustification.Right)) { LengthInBytes = 3 },
                        new ViewDebugElementDto("setBarcodeHeight", BarcodeHeightParameters(101)) { LengthInBytes = 3 },
                        new ViewDebugElementDto("setBarcodeModuleWidth", BarcodeModuleWidthParameters(3)) { LengthInBytes = 3 },
                        new ViewDebugElementDto(
                            "setBarcodeLabelPosition",
                            BarcodeLabelParameters(DomainElements.BarcodeLabelPosition.Below)) { LengthInBytes = 3 },
                        new ViewDebugElementDto("printBarcode") { LengthInBytes = 17 },
                        new ViewImageElementDto(
                            ToViewMediaDto(Media.CreateDefaultPng(1)),
                            0,
                            0,
                            512,
                            101) { LengthInBytes = 17 },
                        new ViewDebugElementDto(
                            "setJustification",
                            JustificationParameters(DomainElements.TextJustification.Left)) { LengthInBytes = 3 },
                        new ViewDebugElementDto("flushLineBufferAndFeed") { LengthInBytes = 1 },
                        new ViewDebugElementDto("pagecut", PagecutParameters(DomainElements.PagecutMode.Partial, 0)) { LengthInBytes = 4 }
                    ]),
                ["case04"] = (
                    expectedRequestElement:
                    [
                        new EscPosElements.Initialize { LengthInBytes = 2 },
                        new EscPosElements.SelectFont(0, false, false) { LengthInBytes = 3 },
                        new EscPosElements.SetCodePage("866") { LengthInBytes = 3 },
                        new EscPosElements.SetJustification(DomainElements.TextJustification.Left) { LengthInBytes = 3 },
                        new EscPosElements.SetQrModel(DomainElements.QrModel.Model2) { LengthInBytes = 9 },
                        new EscPosElements.SetQrModuleSize(7) { LengthInBytes = 8 },
                        new EscPosElements.SetQrErrorCorrection(DomainElements.QrErrorCorrectionLevel.Low) { LengthInBytes = 8 },
                        new EscPosElements.StoreQrData("https://google.com") { LengthInBytes = 26 },
                        new EscPosElements.PrintQrCodeUpload { LengthInBytes = 8 },
                        new EscPosElements.PrintAndLineFeed { LengthInBytes = 1 },
                        new EscPosElements.CutPaper(DomainElements.PagecutMode.Partial, 0) { LengthInBytes = 4 }
                    ],
                    expectedFinalizedElements:
                    [
                        new EscPosElements.Initialize { LengthInBytes = 2 },
                        new EscPosElements.SelectFont(0, false, false) { LengthInBytes = 3 },
                        new EscPosElements.SetCodePage("866") { LengthInBytes = 3 },
                        new EscPosElements.SetJustification(DomainElements.TextJustification.Left) { LengthInBytes = 3 },
                        new EscPosElements.SetQrModel(DomainElements.QrModel.Model2) { LengthInBytes = 9 },
                        new EscPosElements.SetQrModuleSize(7) { LengthInBytes = 8 },
                        new EscPosElements.SetQrErrorCorrection(DomainElements.QrErrorCorrectionLevel.Low) { LengthInBytes = 8 },
                        new EscPosElements.StoreQrData("https://google.com") { LengthInBytes = 26 },
                        new EscPosElements.PrintQrCode("https://google.com", 0, 0, Media.CreateDefaultPng(2)) { LengthInBytes = 8 },
                        new EscPosElements.PrintAndLineFeed { LengthInBytes = 1 },
                        new EscPosElements.CutPaper(DomainElements.PagecutMode.Partial, 0) { LengthInBytes = 4 }
                    ],
                    expectedViewElements:
                    [
                        new ViewDebugElementDto("resetPrinter") { LengthInBytes = 2 },
                        new ViewDebugElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                        new ViewDebugElementDto("setCodePage", CodePageParameters("866")) { LengthInBytes = 3 },
                        new ViewDebugElementDto(
                            "setJustification",
                            JustificationParameters(DomainElements.TextJustification.Left)) { LengthInBytes = 3 },
                        new ViewDebugElementDto("setQrModel", QrModelParameters(DomainElements.QrModel.Model2)) { LengthInBytes = 9 },
                        new ViewDebugElementDto("setQrModuleSize", QrModuleSizeParameters(7)) { LengthInBytes = 8 },
                        new ViewDebugElementDto(
                            "setQrErrorCorrection",
                            QrErrorCorrectionParameters(DomainElements.QrErrorCorrectionLevel.Low)) { LengthInBytes = 8 },
                        new ViewDebugElementDto(
                            "storeQrData",
                            StoreQrDataParameters("https://google.com")) { LengthInBytes = 26 },
                        new ViewDebugElementDto("printQrCode") { LengthInBytes = 8 },
                        new ViewImageElementDto(
                            ToViewMediaDto(Media.CreateDefaultPng(2)),
                            0,
                            0,
                            512,
                            225) { LengthInBytes = 8 },
                        new ViewDebugElementDto("flushLineBufferAndFeed") { LengthInBytes = 1 },
                        new ViewDebugElementDto("pagecut", PagecutParameters(DomainElements.PagecutMode.Partial, 0)) { LengthInBytes = 4 }
                    ]),
                ["case05"] = (
                    expectedRequestElement:
                        [
                            new EscPosElements.Initialize { LengthInBytes = 2 },
                            new EscPosElements.SelectFont(0, false, false) { LengthInBytes = 3 },
                            new EscPosElements.StoredLogo(0) { LengthInBytes = 4 },
                            new EscPosElements.CutPaper(DomainElements.PagecutMode.Partial, 0) { LengthInBytes = 4 }
                        ],
                        expectedFinalizedElements: null, // the same as elements
                        expectedViewElements:
                        [
                            new ViewDebugElementDto("resetPrinter") { LengthInBytes = 2 },
                            new ViewDebugElementDto("setFont", SetFontParameters(0, false, false)) { LengthInBytes = 3 },
                            new ViewDebugElementDto("storedLogo", StoredLogoParameters(0)) { LengthInBytes = 4 },
                            new ViewDebugElementDto("pagecut", PagecutParameters(DomainElements.PagecutMode.Partial, 0)) { LengthInBytes = 4 }
                        ]),
            };

    public static
        IReadOnlyDictionary<string, (
            IReadOnlyList<DomainElements.Element> expectedRequestElement,
            IReadOnlyList<DomainElements.Element>? expectedPersistedElements,
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

    private static IReadOnlyDictionary<string, string> JustificationParameters(DomainElements.TextJustification justification)
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

    private static IReadOnlyDictionary<string, string> BarcodeLabelParameters(DomainElements.BarcodeLabelPosition position)
    {
        return new Dictionary<string, string>
        {
            ["Position"] = position.ToString()
        };
    }

    private static IReadOnlyDictionary<string, string> QrModelParameters(DomainElements.QrModel model)
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

    private static IReadOnlyDictionary<string, string> QrErrorCorrectionParameters(DomainElements.QrErrorCorrectionLevel level)
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

    private static IReadOnlyDictionary<string, string> PagecutParameters(DomainElements.PagecutMode mode, int? feedUnits)
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

}
