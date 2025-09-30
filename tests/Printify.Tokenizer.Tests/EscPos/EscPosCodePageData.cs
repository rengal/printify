namespace Printify.Tokenizer.Tests.EscPos;

using TestServices;
using System;
using System.Collections.Generic;
using System.Text;

internal sealed record CodePageTestVector(
    string CodePage,
    byte[] Command,
    string Uppercase,
    string Lowercase,
    Encoding Encoding
);

internal static class EscPosCodePageData
{
    private const byte Esc = EscPosTokenizer.Esc;

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

    public static IReadOnlyList<CodePageTestVector> All { get; } = Build();

    private static IReadOnlyList<CodePageTestVector> Build()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var vectors = new List<CodePageTestVector>
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
            CreateEsc("1098", 0x29, LatinUpper, LatinLower),
            CreateEsc("1118", 0x2A, LatinUpper, LatinLower),
            CreateEsc("1119", 0x2B, LatinUpper, LatinLower),
            CreateEsc("1125", 0x2C, CyrillicUpper, CyrillicLower),
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

        return vectors;
    }

    private static CodePageTestVector CreateEsc(string codePage, byte parameter, string uppercase, string lowercase)
    {
        var command = new[] { Esc, (byte)'t', parameter };
        return Create(codePage, command, uppercase, lowercase);
    }

    private static CodePageTestVector Create(string codePage, byte[] command, string uppercase, string lowercase)
    {
        try
        {
            var encoding = ResolveEncoding(codePage);
            return new CodePageTestVector(codePage, command, uppercase, lowercase, encoding);
        }
        catch (InvalidOperationException)
        {
            var fallbackEncoding = Encoding.GetEncoding(437);
            return new CodePageTestVector(codePage, command, LatinUpper, LatinLower, fallbackEncoding);
        }
    }

    private static Encoding ResolveEncoding(string codePage)
    {
        try
        {
            if (int.TryParse(codePage, out var numeric))
            {
                return Encoding.GetEncoding(numeric);
            }

            return Encoding.GetEncoding(codePage);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            throw new InvalidOperationException($"Encoding for code page '{codePage}' is not supported on this platform.", ex);
        }
    }
}
