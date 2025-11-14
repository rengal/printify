using System.Text;

namespace Printify.Tests.Shared.EscPos;

using Domain.Documents.Elements;
using Xunit;

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
            ExpectedElements: [new Bell()]),
        new(
            Input: Enumerable.Repeat((byte)0x07, 10).ToArray(),
            ExpectedElements: Enumerable.Range(0, 10).Select(_ => new Bell()).ToArray())
    ];

    public static TheoryData<EscPosScenario> TextScenarios { get; } =
    [
        new(Input: "A"u8.ToArray(), ExpectedElements: [new TextLine("A")]),
        new(Input: "ABC\n"u8.ToArray(), ExpectedElements: [new TextLine("ABC")]),
        new(Input: "ABC"u8.ToArray(), ExpectedElements: [new TextLine("ABC")]),
        new(Input: "ABC"u8.ToArray(), ExpectedElements: [new TextLine("ABC")]),
        new(
            Input: "ABC\nDEF\nG"u8.ToArray(),
            ExpectedElements: [new TextLine("ABC"), new TextLine("DEF"), new TextLine("G")]),
        new(Input: "ABC"u8.ToArray(), ExpectedElements: [new TextLine("ABC")]),
        new(
            Input: Encoding.ASCII.GetBytes(new string('A', 10_000)),
            ExpectedElements: [new TextLine(new string('A', 10_000))]),
        new(
            Input: [.. "ABC"u8, 0x07],
            ExpectedElements: [new TextLine("ABC"), new Bell()]),
        new(
            Input: [.. "ABC"u8, 0x07, .. "DEF"u8, 0x07],
            ExpectedElements: [new TextLine("ABC"), new Bell(), new TextLine("DEF"), new Bell()]),
        new(
            Input: [.. "ABC"u8, 0x07, .. "DEF\n"u8, 0x07],
            ExpectedElements: [new TextLine("ABC"), new Bell(), new TextLine("DEF"), new Bell()])
    ];

    public static TheoryData<EscPosScenario> PagecutScenarios { get; } =
    [
        new(Input: [Esc, (byte)'i'], ExpectedElements: [new Pagecut(PagecutMode.PartialOnePoint)]),
        new(Input: [Gs, 0x56, 0x00], ExpectedElements: [new Pagecut(PagecutMode.Full)]),
        new(Input: [Gs, 0x56, 0x30], ExpectedElements: [new Pagecut(PagecutMode.Full)]),
        new(Input: [Gs, 0x56, 0x01], ExpectedElements: [new Pagecut(PagecutMode.Partial)]),
        new(Input: [Gs, 0x56, 0x31], ExpectedElements: [new Pagecut(PagecutMode.Partial)]),
        new(Input: [Gs, 0x56, 0x41, 0x05], ExpectedElements: [new Pagecut(PagecutMode.Full, 0x05)]),
        new(Input: [Gs, 0x56, 0x42, 0x20], ExpectedElements: [new Pagecut(PagecutMode.Partial, 0x20)]),
        new(Input: [Gs, 0x56, 0x61, 0x05], ExpectedElements: [new Pagecut(PagecutMode.Full, 0x05)]),
        new(Input: [Gs, 0x56, 0x62, 0x20], ExpectedElements: [new Pagecut(PagecutMode.Partial, 0x20)]),
        new(Input: [Gs, 0x56, 0x67, 0x05], ExpectedElements: [new Pagecut(PagecutMode.Full, 0x05)]),
        new(Input: [Gs, 0x56, 0x68, 0x20], ExpectedElements: [new Pagecut(PagecutMode.Partial, 0x20)])
    ];

    public static TheoryData<EscPosScenario> PulseScenarios { get; } =
    [
        new(
            Input: [Esc, (byte)'p', 0x01, 0x05, 0x0A],
            ExpectedElements: [new Pulse(PulsePin.Drawer2, 0x05, 0x0A)]),
        new(
            Input: [Esc, (byte)'p', 0x00, 0x7D, 0x7F],
            ExpectedElements: [new Pulse(PulsePin.Drawer1, 0x7D, 0x7F)]),
        new(
            Input:
            [
                Esc, (byte)'p', 0x00, 0x08, 0x16,
                Esc, (byte)'p', 0x01, 0x02, 0x03
            ],
            ExpectedElements:
            [
                new Pulse(PulsePin.Drawer1, 0x08, 0x16),
                new Pulse(PulsePin.Drawer2, 0x02, 0x03)
            ])
    ];

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
            ExpectedElements:
            [
                new SetFont(0, false, false),
                new SetFont(1, false, false),
                new SetFont(0, true, false),
                new SetFont(1, true, true),
                new SetFont(2, false, false)
            ]),
        new(
            Input:
            [
                Esc, (byte)'E', 0x01,
                Esc, (byte)'E', 0x00,
                Esc, (byte)'E', 0x01,
                Esc, (byte)'E', 0x00
            ],
            ExpectedElements:
            [
                new SetBoldMode(true),
                new SetBoldMode(false),
                new SetBoldMode(true),
                new SetBoldMode(false)
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
            ExpectedElements:
            [
                new SetUnderlineMode(true),
                new SetUnderlineMode(true),
                new SetUnderlineMode(false),
                new SetReverseMode(true),
                new SetReverseMode(false),
                new SetReverseMode(true)
            ])
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
                expected.Add(new SetCodePage(vector.CodePage));
            }

            void AppendText(string text)
            {
                var bytes = vector.Encoding.GetBytes(text);
                input.AddRange(bytes);
                input.Add(Lf);

                var normalized = vector.Encoding.GetString(bytes);
                expected.Add(new TextLine(normalized));
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
