using Microsoft.AspNetCore.Mvc.Testing;
using Printify.Domain.Documents.Elements;

namespace Printify.Web.Tests.EscPos;

public class EscPosGoldenTests(WebApplicationFactory<Program> factory) : EscPosTests(factory)
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<Element>> Expectations = new Dictionary<string, IReadOnlyList<Element>>(StringComparer.OrdinalIgnoreCase)
    {
        ["case01"] =
        [
            new ResetPrinter(),
            new SetFont(0, false, false),
            new SetCodePage("866"),
            new SetFont(0, false, false),
            new TextLine(Pad("font 0", 42)),
            new Pagecut()
        ],
        ["case02"] =
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
            new Pagecut()
        ],
        ["case03"] =
        [
            new ResetPrinter(),
            new SetFont(0, false, false),
            new SetCodePage("866"),
            new SetFont(0, false, false),
            new SetJustification(TextJustification.Right),
            new SetBarcodeHeight(101),
            new SetBarcodeModuleWidth(3),
            new SetBarcodeLabelPosition(BarcodeLabelPosition.Below),
            new PrintBarcode(BarcodeSymbology.Ean13, "1234567890128"),
            new SetJustification(TextJustification.Left),
            new TextLine(string.Empty),
            new Pagecut()
        ],
        ["case04"] =
        [
            new ResetPrinter(),
            new SetFont(0, false, false),
            new SetCodePage("866"),
            new SetJustification(TextJustification.Left),
            new SetQrModel(QrModel.Model2),
            new SetQrModuleSize(7),
            new SetQrErrorCorrection(QrErrorCorrectionLevel.Low),
            new StoreQrData("https://google.com"),
            new PrintQrCode("https://google.com"),
            new TextLine(string.Empty),
            new Pagecut()
        ],
        ["case05"] =
        [
            new ResetPrinter(),
            new SetFont(0, false, false),
            new StoredLogo(0),
            new Pagecut()
        ]
    };

    public static TheoryData<string, byte[]> GoldenCases => BuildGoldenCases();

    private static TheoryData<string, byte[]> BuildGoldenCases()
    {
        var data = new TheoryData<string, byte[]>();
        var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "EscPos"));
        if (!Directory.Exists(directory))
        {
            return data;
        }

        foreach (var path in Directory.EnumerateFiles(directory, "case*.b64").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            var base64 = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(base64))
            {
                continue;
            }

            try
            {
                var bytes = Convert.FromBase64String(base64);
                data.Add(Path.GetFileNameWithoutExtension(path), bytes);
            }
            catch (FormatException)
            {
                // Ignore malformed placeholders; content will be filled later.
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(GoldenCases))]
    public async Task EscPos_Golden_Cases_ProduceExpectedDocuments(string caseId, byte[] payload)
    {
        Assert.True(Expectations.TryGetValue(caseId, out var expectedElements));

        var scenario = new EscPosScenario(payload, expectedElements);
        await RunScenarioAsync(scenario);
    }

    private static string Pad(string text, int totalLength) => text.PadRight(totalLength);
}
