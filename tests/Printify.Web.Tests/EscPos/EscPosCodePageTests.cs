using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Printify.Domain.Documents.Elements;

namespace Printify.Web.Tests.EscPos;

public class EscPosCodePageTests: EscPosTests
{
    public EscPosCodePageTests(WebApplicationFactory<Program> factory) : base(factory)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private static IReadOnlyList<CodePageVector> CodePageVectors { get; } = BuildCodePageVectors();

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
            //CreateEsc("1098", 0x29, LatinUpper, LatinLower),
            //CreateEsc("1118", 0x2A, LatinUpper, LatinLower),
            //CreateEsc("1119", 0x2B, LatinUpper, LatinLower),
            //CreateEsc("1125", 0x2C, CyrillicUpper, CyrillicLower),
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

    [Fact]
    public async Task EscPos_CodePage_Scenarios_ProduceExpectedDocuments()
    {
        var scenarios = BuildCodePageScenarios();
        foreach (var scenario in scenarios)
            await RunScenarioAsync(scenario);
    }
    
    private static TheoryData<EscPosScenario> BuildCodePageScenarios()
    {
        var scenarios = new TheoryData<EscPosScenario>();
        foreach (var vector in CodePageVectors)
        {
            var input = new List<byte>();
            var expected = new List<Element>();
            var sequence = 0;
    
            if (vector.Command.Length > 0)
            {
                input.AddRange(vector.Command);
                expected.Add(new SetCodePage(++sequence, vector.CodePage));
            }
    
            void AppendText(string text)
            {
                var bytes = vector.Encoding.GetBytes(text);
                input.AddRange(bytes);
                input.Add(Lf);
    
                var normalized = vector.Encoding.GetString(bytes);
                expected.Add(new TextLine(++sequence, normalized));
            }
    
            AppendText(vector.Uppercase);
            AppendText(vector.Lowercase);
    
            scenarios.Add(new EscPosScenario(input.ToArray(), expected));
        }
    
        return scenarios;
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
    
    private const byte Esc = 0x1B;
    private const byte Lf = 0x0A;
    
    private const string LatinUpper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string LatinLower = "abcdefghijklmnopqrstuvwxyz";
    private const string GreekUpper = "ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩ";
    private const string GreekLower = "αβγδεζηθικλμνξοπρστυφχψως";
    private const string CyrillicUpper = "АБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
    private const string CyrillicLower = "абвгдежзийклмнопрстуфхцчшщъыьэюя";
    private const string TurkishUpper = "ABCÇDEFGĞHIİJKLMNOÖPRSŞTUÜVYZ";
    private const string TurkishLower = "abcçdefgğhıijklmnoöprsştuüvyz";
    private const string HebrewLetters = "אבגדהוזחטיךכלםמןנסעףפץצקרשת";
    private const string ArabicLetters = "ابتثجحخدذرزسشصضطظعغفقكلمنهوي";
}
