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
                        new ResetPrinter(),
                        new SetFont(0, false, false),
                        new SetCodePage("866"),
                        new SetFont(0, false, false),
                        new TextLine(Pad("font 0", 42)),
                        new Pagecut(PagecutMode.Partial, 0)
                    ],
                    expectedFinalizedElements: null), // the same as elements
                ["case02"] = (
                    expectedRequestElement:
                    [
                        new ResetPrinter(),
                        new SetFont(0, false, false),
                        new SetCodePage("866"),
                        new SetFont(0, false, false),
                        new TextLine(Pad("font 0", 42)),
                        new SetFont(1, true, true),
                        new TextLine(Pad("font 1", 28)),
                        new SetFont(0, true, true),
                        new TextLine(Pad("font 2", 21)),
                        new Pagecut(PagecutMode.Partial, 0)
                    ],
                    expectedFinalizedElements: null), // the same as elements
                ["case03"] = (
                    expectedRequestElement:
                    [
                        new ResetPrinter(),
                        new SetFont(0, false, false),
                        new SetCodePage("866"),
                        new SetFont(0, false, false),
                        new SetJustification(TextJustification.Right),
                        new SetBarcodeHeight(101),
                        new SetBarcodeModuleWidth(3),
                        new SetBarcodeLabelPosition(BarcodeLabelPosition.Below),
                        new PrintBarcodeUpload(BarcodeSymbology.Ean13, "1234567890128"),
                        new SetJustification(TextJustification.Left),
                        new TextLine(string.Empty),
                        new Pagecut(PagecutMode.Partial, 0)
                    ],
                    expectedFinalizedElements:
                    [
                        new ResetPrinter(),
                        new SetFont(0, false, false),
                        new SetCodePage("866"),
                        new SetFont(0, false, false),
                        new SetJustification(TextJustification.Right),
                        new SetBarcodeHeight(101),
                        new SetBarcodeModuleWidth(3),
                        new SetBarcodeLabelPosition(BarcodeLabelPosition.Below),
                        new PrintBarcode(BarcodeSymbology.Ean13, "1234567890128", 0, 0, Media.CreateDefaultPng(1)),
                        new SetJustification(TextJustification.Left),
                        new TextLine(string.Empty),
                        new Pagecut(PagecutMode.Partial, 0)
                    ]),
                ["case04"] = (
                    expectedRequestElement:
                    [
                        new ResetPrinter(),
                        new SetFont(0, false, false),
                        new SetCodePage("866"),
                        new SetJustification(TextJustification.Left),
                        new SetQrModel(QrModel.Model2),
                        new SetQrModuleSize(7),
                        new SetQrErrorCorrection(QrErrorCorrectionLevel.Low),
                        new StoreQrData("https://google.com"),
                        new PrintQrCodeUpload(),
                        new TextLine(string.Empty),
                        new Pagecut(PagecutMode.Partial, 0)
                    ],
                    expectedFinalizedElements:
                    [
                        new ResetPrinter(),
                        new SetFont(0, false, false),
                        new SetCodePage("866"),
                        new SetJustification(TextJustification.Left),
                        new SetQrModel(QrModel.Model2),
                        new SetQrModuleSize(7),
                        new SetQrErrorCorrection(QrErrorCorrectionLevel.Low),
                        new StoreQrData("https://google.com"),
                        new PrintQrCode("https://google.com", 0, 0, Media.CreateDefaultPng(2)),
                        new TextLine(string.Empty),
                        new Pagecut(PagecutMode.Partial, 0)
                    ]), // the same as elements
                ["case05"] = (
                    expectedRequestElement:
                        [
                            new ResetPrinter(),
                            new SetFont(0, false, false),
                            new StoredLogo(0),
                            new Pagecut(PagecutMode.Partial, 0)
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
