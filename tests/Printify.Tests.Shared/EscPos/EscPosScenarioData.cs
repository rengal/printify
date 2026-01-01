using System.Text;
using Printify.Domain.Media;
using Printify.Infrastructure.Media;
using Printify.Domain.Documents.Elements;
using Xunit;

namespace Printify.Tests.Shared.EscPos;

/// <summary>
/// Provides reusable ESC/POS parser scenarios for unit and integration tests.
/// </summary>
public static class EscPosScenarioData
{
    private const byte Esc = 0x1B;
    private const byte Gs = 0x1D;
    private const byte Lf = 0x0A;

    static EscPosScenarioData()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var codePageVectors = BuildCodePageVectors();
        CodePageScenarios = BuildCodePageScenarios(codePageVectors);
    }

    public static TheoryData<EscPosScenario> BellScenarios { get; } =
    [
        new(Input: [0x07],
            ExpectedRequestElements: [new Bell { LengthInBytes = 1 }]),
        new(
            Input: Enumerable.Repeat((byte)0x07, 10).ToArray(),
            ExpectedRequestElements: Enumerable.Range(0, 10).Select(_ => new Bell { LengthInBytes = 1 }).ToArray())
    ];

    public static TheoryData<EscPosScenario> TextScenarios { get; } =
    [
        new(Input: "A"u8.ToArray(), ExpectedRequestElements: [new AppendToLineBuffer("A") { LengthInBytes = 1 }]),
        new(Input: "ABC\n"u8.ToArray(), ExpectedRequestElements: [
            new AppendToLineBuffer("ABC") { LengthInBytes = 3 },
            new FlushLineBufferAndFeed { LengthInBytes = 1 }]),
        new(Input: "ABC"u8.ToArray(), ExpectedRequestElements: [new AppendToLineBuffer("ABC") { LengthInBytes = 3 }]),
        new(Input: "ABC"u8.ToArray(), ExpectedRequestElements: [new AppendToLineBuffer("ABC") { LengthInBytes = 3 }]),
        new(
            Input: "ABC\nDEF\nG"u8.ToArray(),
            ExpectedRequestElements: [
                new AppendToLineBuffer("ABC") { LengthInBytes = 3 },
                new FlushLineBufferAndFeed { LengthInBytes = 1 },
                new AppendToLineBuffer("DEF") { LengthInBytes = 3 },
                new FlushLineBufferAndFeed { LengthInBytes = 1 },
                new AppendToLineBuffer("G") { LengthInBytes = 1 }]),
        new(Input: "ABC"u8.ToArray(), ExpectedRequestElements: [new AppendToLineBuffer("ABC") { LengthInBytes = 3 }]),
        new(
            Input: Encoding.ASCII.GetBytes(new string('A', 10_000)),
            ExpectedRequestElements: [new AppendToLineBuffer(new string('A', 10_000)) { LengthInBytes = 10_000 }]),
        new(
            Input: [.. "ABC"u8, 0x07],
            ExpectedRequestElements: [new AppendToLineBuffer("ABC") { LengthInBytes = 3 }, new Bell { LengthInBytes = 1 }]),
        new(
            Input: [.. "ABC"u8, 0x07, .. "DEF"u8, 0x07],
            ExpectedRequestElements: [
                new AppendToLineBuffer("ABC") { LengthInBytes = 3 },
                new Bell { LengthInBytes = 1 },
                new AppendToLineBuffer("DEF") { LengthInBytes = 3 },
                new Bell { LengthInBytes = 1 }]),
        new(
            Input: [.. "ABC"u8, 0x07, .. "DEF\n"u8, 0x07],
            ExpectedRequestElements: [
                new AppendToLineBuffer("ABC") { LengthInBytes = 3 },
                new Bell { LengthInBytes = 1 },
                new AppendToLineBuffer("DEF") { LengthInBytes = 3 },
                new FlushLineBufferAndFeed { LengthInBytes = 1 },
                new Bell { LengthInBytes = 1 }]),
        new(
            Input: "\n"u8.ToArray(),
            ExpectedRequestElements: [new FlushLineBufferAndFeed { LengthInBytes = 1 }]),
        new(
            Input: "\n\n\n"u8.ToArray(),
            ExpectedRequestElements: [
                new FlushLineBufferAndFeed { LengthInBytes = 1 },
                new FlushLineBufferAndFeed { LengthInBytes = 1 },
                new FlushLineBufferAndFeed { LengthInBytes = 1 }
            ])
    ];

    public static TheoryData<EscPosScenario> ErrorScenarios { get; } =
    [
        // Single null byte produces one error
        new(
            Input: [0x00],
            ExpectedRequestElements: [new PrinterError("") { LengthInBytes = 1 }]),
        // Two consecutive null bytes produce one error (accumulated)
        new(
            Input: [0x00, 0x00],
            ExpectedRequestElements: [new PrinterError("") { LengthInBytes = 2 }]),
        // Multiple invalid bytes produce one error
        new(
            Input: [0x00, 0x01, 0x02],
            ExpectedRequestElements: [new PrinterError("") { LengthInBytes = 3 }]),
        // Invalid byte followed by text transitions correctly
        new(
            Input: [0x00, .. "ABC"u8],
            ExpectedRequestElements: [
                new PrinterError("") { LengthInBytes = 1 },
                new AppendToLineBuffer("ABC") { LengthInBytes = 3 }]),
        // Text followed by invalid byte followed by text
        new(
            Input: [.. "ABC"u8, 0x00, .. "DEF"u8],
            ExpectedRequestElements: [
                new AppendToLineBuffer("ABC") { LengthInBytes = 3 },
                new PrinterError("") { LengthInBytes = 1 },
                new AppendToLineBuffer("DEF") { LengthInBytes = 3 }]),
        // Invalid byte followed by command
        new(
            Input: [0x00, 0x07],
            ExpectedRequestElements: [
                new PrinterError("") { LengthInBytes = 1 },
                new Bell { LengthInBytes = 1 }])
    ];

    public static TheoryData<EscPosScenario> PagecutScenarios { get; } =
    [
        new(Input: [Esc, (byte)'i'], ExpectedRequestElements: [new Pagecut(PagecutMode.PartialOnePoint) { LengthInBytes = 2 }]),
        new(Input: [Gs, 0x56, 0x00], ExpectedRequestElements: [new Pagecut(PagecutMode.Full) { LengthInBytes = 3 }]),
        new(Input: [Gs, 0x56, 0x30], ExpectedRequestElements: [new Pagecut(PagecutMode.Full) { LengthInBytes = 3 }]),
        new(Input: [Gs, 0x56, 0x01], ExpectedRequestElements: [new Pagecut(PagecutMode.Partial) { LengthInBytes = 3 }]),
        new(Input: [Gs, 0x56, 0x31], ExpectedRequestElements: [new Pagecut(PagecutMode.Partial) { LengthInBytes = 3 }]),
        new(Input: [Gs, 0x56, 0x41, 0x05], ExpectedRequestElements: [new Pagecut(PagecutMode.Full, 0x05) { LengthInBytes = 4 }]),
        new(Input: [Gs, 0x56, 0x42, 0x20], ExpectedRequestElements: [new Pagecut(PagecutMode.Partial, 0x20) { LengthInBytes = 4 }]),
        new(Input: [Gs, 0x56, 0x61, 0x05], ExpectedRequestElements: [new Pagecut(PagecutMode.Full, 0x05) { LengthInBytes = 4 }]),
        new(Input: [Gs, 0x56, 0x62, 0x20], ExpectedRequestElements: [new Pagecut(PagecutMode.Partial, 0x20) { LengthInBytes = 4 }]),
        new(Input: [Gs, 0x56, 0x67, 0x05], ExpectedRequestElements: [new Pagecut(PagecutMode.Full, 0x05) { LengthInBytes = 4 }]),
        new(Input: [Gs, 0x56, 0x68, 0x20], ExpectedRequestElements: [new Pagecut(PagecutMode.Partial, 0x20) { LengthInBytes = 4 }])
    ];

    public static TheoryData<EscPosScenario> PulseScenarios { get; } =
    [
        new(
            Input: [Esc, (byte)'p', 0x01, 0x05, 0x0A],
            ExpectedRequestElements: [new Pulse(1, 0x05, 0x0A) { LengthInBytes = 5 }]),
        new(
            Input: [Esc, (byte)'p', 0x00, 0x7D, 0x7F],
            ExpectedRequestElements: [new Pulse(0, 0x7D, 0x7F) { LengthInBytes = 5 }]),
        new(
            Input:
            [
                Esc, (byte)'p', 0x00, 0x08, 0x16,
                Esc, (byte)'p', 0x01, 0x02, 0x03
            ],
            ExpectedRequestElements:
            [
                new Pulse(0, 0x08, 0x16) { LengthInBytes = 5 },
                new Pulse(1, 0x02, 0x03) { LengthInBytes = 5 }
            ])
    ];

    public static TheoryData<EscPosScenario> RasterImageScenarios { get; } =
    [
        // GS v 0: Print raster bit image - 8x2 partially set (with pixel verification)
        // Row 0: 11100000 (3 colored, 5 transparent)
        // Row 1: 00011000 (2 colored at positions 3-4)
        new(
            Input:
            [
                Gs, (byte)'v', 0x30, 0x00, // GS v 0 m: Print raster, m=0 (normal mode)
                0x01, 0x00, // xL xH: width in bytes (1 byte = 8 dots)
                0x02, 0x00, // yL yH: height in dots (2 rows)
                0b11100000, // Row 0: XXX_____ (X=colored/set, _=transparent/unset)
                0b00011000  // Row 1: ___XX___ (X=colored/set, _=transparent/unset)
            ],
            ExpectedRequestElements:
            [
                new RasterImageUpload(
                    Width: 8,
                    Height: 2,
                    Media: CreateExpectedRasterMedia(8, 2, [0b11100000, 0b00011000]))
                { LengthInBytes = 10 }
            ],
            ExpectedPersistedElements:
            [
                new RasterImage(8, 2, Media.CreateDefaultPng(112)) { LengthInBytes = 10 }
            ]),

        // GS v 0: All bits set (8x2, all colored pixels)
        new(
            Input:
            [
                Gs, (byte)'v', 0x30, 0x00,
                0x01, 0x00, // width: 1 byte = 8 pixels
                0x02, 0x00, // height: 2 rows
                0xFF,       // Row 0: all colored
                0xFF        // Row 1: all colored
            ],
            ExpectedRequestElements:
            [
                new RasterImageUpload(
                    Width: 8,
                    Height: 2,
                    Media: CreateExpectedRasterMedia(8, 2, [0xFF, 0xFF]))
                { LengthInBytes = 10 }
            ],
            ExpectedPersistedElements:
            [
                new RasterImage(8, 2, Media.CreateDefaultPng(97)) { LengthInBytes = 10 }
            ]),

        // GS v 0: All bits unset (8x2, all transparent pixels)
        new(
            Input:
            [
                Gs, (byte)'v', 0x30, 0x00,
                0x01, 0x00, // width: 1 byte = 8 pixels
                0x02, 0x00, // height: 2 rows
                0x00,       // Row 0: all transparent
                0x00        // Row 1: all transparent
            ],
            ExpectedRequestElements:
            [
                new RasterImageUpload(
                    Width: 8,
                    Height: 2,
                    Media: CreateExpectedRasterMedia(8, 2, [0x00, 0x00]))
                { LengthInBytes = 10 }
            ],
            ExpectedPersistedElements:
            [
                new RasterImage(8, 2, Media.CreateDefaultPng(97)) { LengthInBytes = 10 }
            ]),

        // GS v 0: Checkerboard pattern (8x2)
        new(
            Input:
            [
                Gs, (byte)'v', 0x30, 0x00,
                0x01, 0x00, // width: 1 byte = 8 pixels
                0x02, 0x00, // height: 2 rows
                0b10101010, // Row 0: X_X_X_X_
                0b01010101  // Row 1: _X_X_X_X
            ],
            ExpectedRequestElements:
            [
                new RasterImageUpload(
                    Width: 8,
                    Height: 2,
                    Media: CreateExpectedRasterMedia(8, 2, [0b10101010, 0b01010101]))
                { LengthInBytes = 10 }
            ],
            ExpectedPersistedElements:
            [
                new RasterImage(8, 2, Media.CreateDefaultPng(101)) { LengthInBytes = 10 }
            ])
    ];

    /// <summary>
    /// Creates expected raster image media by converting MonochromeBitmap to PNG.
    /// This generates the exact expected output for pixel verification.
    /// </summary>
    private static MediaUpload CreateExpectedRasterMedia(int width, int height, byte[] bitmapData)
    {
        var bitmap = new MonochromeBitmap(width, height, bitmapData);
        var mediaService = new MediaService();
        return mediaService.ConvertToMediaUpload(bitmap);
    }

    public static TheoryData<EscPosScenario> FontStyleScenarios { get; } =
    [
        new(
            Input:
            [
                Esc, 0x21, 0x00,
                Esc, 0x21, 0x01,
                Esc, 0x21, 0x20,
                Esc, 0x21, 0x31,
                Esc, 0x21, 0x02
            ],
            ExpectedRequestElements:
            [
                new SetFont(0, false, false) { LengthInBytes = 3 },
                new SetFont(1, false, false) { LengthInBytes = 3 },
                new SetFont(0, true, false) { LengthInBytes = 3 },
                new SetFont(1, true, true) { LengthInBytes = 3 },
                new SetFont(2, false, false) { LengthInBytes = 3 }
            ]),
        new(
            Input:
            [
                Esc, (byte)'E', 0x01,
                Esc, (byte)'E', 0x00,
                Esc, (byte)'E', 0x01,
                Esc, (byte)'E', 0x00
            ],
            ExpectedRequestElements:
            [
                new SetBoldMode(true) { LengthInBytes = 3 },
                new SetBoldMode(false) { LengthInBytes = 3 },
                new SetBoldMode(true) { LengthInBytes = 3 },
                new SetBoldMode(false) { LengthInBytes = 3 }
            ]),
        new(
            Input:
            [
                Esc, 0x2D, 0x01,
                Esc, 0x2D, 0x02,
                Esc, 0x2D, 0x00,
                Gs, 0x42, 0x01,
                Gs, 0x42, 0x00,
                Gs, 0x42, 0x01
            ],
            ExpectedRequestElements:
            [
                new SetUnderlineMode(true) { LengthInBytes = 3 },
                new SetUnderlineMode(true) { LengthInBytes = 3 },
                new SetUnderlineMode(false) { LengthInBytes = 3 },
                new SetReverseMode(true) { LengthInBytes = 3 },
                new SetReverseMode(false) { LengthInBytes = 3 },
                new SetReverseMode(true) { LengthInBytes = 3 }
            ])
    ];

    public static TheoryData<EscPosScenario> LineSpacingScenarios { get; } =
    [
        new(
            Input: [Esc, 0x33, 0x40],
            ExpectedRequestElements: [new SetLineSpacing(0x40) { LengthInBytes = 3 }]),
        new(
            Input: [Esc, 0x32],
            ExpectedRequestElements: [new ResetLineSpacing() { LengthInBytes = 2 }])
    ];

    public static TheoryData<EscPosScenario> CodePageScenarios { get; }

    private static TheoryData<EscPosScenario> BuildCodePageScenarios(IReadOnlyList<CodePageVector> codePages)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var scenarios = new TheoryData<EscPosScenario>();
        foreach (var vector in codePages)
        {
            var input = new List<byte>();
            var expected = new List<Element>();

            if (vector.Command.Length > 0)
            {
                input.AddRange(vector.Command);
                expected.Add(new SetCodePage(vector.CodePage) { LengthInBytes = vector.Command.Length });
            }

            void AppendText(string text)
            {
                var bytes = vector.Encoding.GetBytes(text);
                input.AddRange(bytes);
                input.Add(Lf);

                var normalized = vector.Encoding.GetString(bytes);
                expected.Add(new AppendToLineBuffer(normalized) { LengthInBytes = bytes.Length });
                expected.Add(new FlushLineBufferAndFeed { LengthInBytes = 1 });
            }

            AppendText(vector.Uppercase);
            AppendText(vector.Lowercase);

            scenarios.Add(new EscPosScenario(input.ToArray(), expected));
        }

        return scenarios;
    }

    private static IReadOnlyList<CodePageVector> BuildCodePageVectors()
    {
        return new List<CodePageVector>
        {
            CreateEsc("437", 0x00, LatinUpper, LatinLower),
            CreateEsc("720", 0x20, ArabicLetters, ArabicLetters),
            CreateEsc("737", 0x0E, GreekUpper, GreekLower),
            CreateEsc("775", 0x21, LatinUpper, LatinLower),
            CreateEsc("850", 0x02, LatinUpper, LatinLower),
            CreateEsc("852", 0x12, LatinUpper, LatinLower),
            CreateEsc("855", 0x22, CyrillicUpper, CyrillicLower),
            CreateEsc("857", 0x0D, TurkishUpper, TurkishLower),
            CreateEsc("858", 0x13, LatinUpper, LatinLower),
            CreateEsc("860", 0x03, LatinUpper, LatinLower),
            CreateEsc("861", 0x23, LatinUpper, LatinLower),
            CreateEsc("862", 0x24, HebrewLetters, HebrewLetters),
            CreateEsc("863", 0x04, LatinUpper, LatinLower),
            CreateEsc("864", 0x25, ArabicLetters, ArabicLetters),
            CreateEsc("865", 0x05, LatinUpper, LatinLower),
            CreateEsc("866", 0x11, CyrillicUpper, CyrillicLower),
            CreateEsc("869", 0x26, GreekUpper, GreekLower),
            CreateEsc("1250", 0x2D, LatinUpper, LatinLower),
            CreateEsc("1251", 0x2E, CyrillicUpper, CyrillicLower),
            CreateEsc("1252", 0x10, LatinUpper, LatinLower),
            CreateEsc("1253", 0x2F, GreekUpper, GreekLower),
            CreateEsc("1254", 0x30, TurkishUpper, TurkishLower),
            CreateEsc("1255", 0x31, HebrewLetters, HebrewLetters),
            CreateEsc("1256", 0x32, ArabicLetters, ArabicLetters),
            CreateEsc("1257", 0x33, LatinUpper, LatinLower),
            CreateEsc("1258", 0x34, LatinUpper, LatinLower)
        };
    }

    private static CodePageVector CreateEsc(string codePage, byte parameter, string uppercase, string lowercase)
    {
        var command = new[] { Esc, (byte)'t', parameter };
        return Create(codePage, command, uppercase, lowercase);
    }

    private static CodePageVector Create(string codePage, byte[] command, string uppercase, string lowercase)
    {
        try
        {
            var encoding = ResolveEncoding(codePage);
            return new CodePageVector(codePage, command, uppercase, lowercase, encoding);
        }
        catch (InvalidOperationException)
        {
            var fallback = Encoding.GetEncoding(437);
            return new CodePageVector(codePage, command, LatinUpper, LatinLower, fallback);
        }
    }

    private static Encoding ResolveEncoding(string codePage)
    {
        if (int.TryParse(codePage, out var numeric))
        {
            return Encoding.GetEncoding(numeric);
        }

        return Encoding.GetEncoding(codePage);
    }

    private sealed record CodePageVector(
        string CodePage,
        byte[] Command,
        string Uppercase,
        string Lowercase,
        Encoding Encoding);

    private const string LatinUpper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string LatinLower = "abcdefghijklmnopqrstuvwxyz";
    private const string GreekUpper = "ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩ";
    private const string GreekLower = "αβγδεζηθικλμνξοπρστυφχψω";
    private const string CyrillicUpper = "АБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
    private const string CyrillicLower = "абвгдежзийклмнопрстуфхцчшщъыьэюя";
    private const string TurkishUpper = "ABCÇDEFGĞHIİJKLMNOÖPRSŞTUÜVYZ";
    private const string TurkishLower = "abcçdefgğhıijklmnoöprsştuüvyz";
    private const string HebrewLetters = "אבגדהוזחטיךכלםמןנסעףפץצקרשת";
    private const string ArabicLetters = "ابتثجحخدذرزسشصضطظعغفقكلمنهوي";
}
