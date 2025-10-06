using Printify.Services.Tokenizer;
using Printify.TestServices;
using Printify.Contracts;
using Printify.Contracts.Documents.Elements;

namespace Printify.Tokenizer.Tests.EscPos;

public sealed class GoldenTests
{
    private static readonly IReadOnlyDictionary<string, Element[]> Expectations = new Dictionary<string, Element[]>(StringComparer.OrdinalIgnoreCase)
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

    private static string Pad(string text, int totalLength)
    {
        return text.PadRight(totalLength);
    }

    public static IEnumerable<object[]> Cases
    {
        get
        {
            var dataDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "EscPos"));
            if (!Directory.Exists(dataDirectory))
            {
                yield break;
            }

            foreach (var path in Directory.EnumerateFiles(dataDirectory, "case*.b64").OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                yield return
                [
                    Path.GetFileNameWithoutExtension(path),
                    File.ReadAllText(path)
                ];
            }
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ParsesGoldenCases(string caseId, string base64)
    {
        using var context = TestServiceContext.Create(tokenizer: typeof(EscPosTokenizer));

        Assert.NotNull(context.Tokenizer);

        var session = context.Tokenizer.CreateSession();

        var bytes = Convert.FromBase64String(base64);
        session.Feed(bytes);
        session.Complete(CompletionReason.DataTimeout);

        Assert.True(Expectations.TryGetValue(caseId, out var expectedElements));

        DocumentAssertions.Equal(
            session.Document,
            Protocol.EscPos,
            expectedElements);
    }
}
