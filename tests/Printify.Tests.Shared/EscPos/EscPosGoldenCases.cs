using System.Text;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Media;
using Xunit;

namespace Printify.Tests.Shared.EscPos;

/// <summary>
/// Provides shared ESC/POS golden cases and their expected documents.
/// </summary>
public static class EscPosGoldenCases
{
    private static readonly
        IReadOnlyDictionary<string, (IReadOnlyList<Element>, IReadOnlyList<Element>?)> expectations =
            new Dictionary<string, (IReadOnlyList<Element> expectedRequestElement, IReadOnlyList<Element>?
                expectedFinalizedElements)>
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
                    expectedFinalizedElements: null), // the same as elements
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
                    expectedFinalizedElements: null), // the same as elements
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
                    ]), // the same as elements
                ["case05"] = (
                    expectedRequestElement:
                        [
                            new ResetPrinter { LengthInBytes = 2 },
                            new SetFont(0, false, false) { LengthInBytes = 3 },
                            new StoredLogo(0) { LengthInBytes = 4 },
                            new Pagecut(PagecutMode.Partial, 0) { LengthInBytes = 4 }
                        ],
                        expectedFinalizedElements: null), // the same as elements
            };

    public static
        IReadOnlyDictionary<string, (IReadOnlyList<Element> expectedRequestElement, IReadOnlyList<Element> expectedPersistedElements)> Expectations => expectations;

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
}
