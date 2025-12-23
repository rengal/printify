namespace Printify.Infrastructure.Printing.EscPos;

internal static class EscPosTextByteRules
{
    public static bool IsTextByte(byte value)
    {
        return value >= 0x20 && value != 0x7F;
    }
}
