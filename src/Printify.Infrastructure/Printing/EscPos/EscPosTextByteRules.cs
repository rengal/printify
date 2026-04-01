namespace Printify.Infrastructure.Printing.EscPos;

internal static class EscPosTextByteRules
{
    public static bool IsTextByte(byte value)
    {
        // Printable single-byte code pages can legitimately use 0xFF for glyphs such as "я" in Windows-1251.
        return value >= 0x20 && value != 0x7F;
    }
}
