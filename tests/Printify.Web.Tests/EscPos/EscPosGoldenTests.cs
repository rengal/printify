using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Testing;
using Printify.Domain.Documents.Elements;
using Printify.Domain.Printers;

namespace Printify.Web.Tests.EscPos;

public class EscPosGoldenTests(WebApplicationFactory<Program> factory) : EscPosTests(factory)
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<Element>> Expectations = new Dictionary<string, IReadOnlyList<Element>>(StringComparer.OrdinalIgnoreCase)
    {
        ["case01"] =
        [
            new ResetPrinter(1),
            new SetFont(2, 0, false, false),
            new SetCodePage(3, "866"),
            new SetFont(4, 0, false, false),
            new TextLine(5, Pad("font 0", 42)),
            new Pagecut(6)
        ],
        ["case02"] =
        [
            new ResetPrinter(1),
            new SetFont(2, 0, false, false),
            new SetCodePage(3, "866"),
            new SetFont(4, 0, false, false),
            new TextLine(5, Pad("font 0", 42)),
            new SetFont(6, 1, true, true),
            new TextLine(7, Pad("font 1", 28)),
            new SetFont(8, 0, true, true),
            new TextLine(9, Pad("font 2", 21)),
            new Pagecut(10)
        ],
        ["case03"] =
        [
            new ResetPrinter(1),
            new SetFont(2, 0, false, false),
            new SetCodePage(3, "866"),
            new SetFont(4, 0, false, false),
            new SetJustification(5, TextJustification.Right),
            new SetBarcodeHeight(6, 101),
            new SetBarcodeModuleWidth(7, 3),
            new SetBarcodeLabelPosition(8, BarcodeLabelPosition.Below),
            new PrintBarcode(9, BarcodeSymbology.Ean13, "1234567890128"),
            new SetJustification(10, TextJustification.Left),
            new TextLine(11, string.Empty),
            new Pagecut(12)
        ],
        ["case04"] =
        [
            new ResetPrinter(1),
            new SetFont(2, 0, false, false),
            new SetCodePage(3, "866"),
            new SetJustification(4, TextJustification.Left),
            new SetQrModel(5, QrModel.Model2),
            new SetQrModuleSize(6, 7),
            new SetQrErrorCorrection(7, QrErrorCorrectionLevel.Low),
            new StoreQrData(8, "https://google.com"),
            new PrintQrCode(9, "https://google.com"),
            new TextLine(10, string.Empty),
            new Pagecut(11)
        ],
        ["case05"] =
        [
            new ResetPrinter(1),
            new SetFont(2, 0, false, false),
            new StoredLogo(3, 0),
            new Pagecut(4)
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
