using System.Text;
using Printify.Infrastructure.Printing.Common;
using Printify.Infrastructure.Printing.EscPos.Commands;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// <summary>
/// ASCII: GS h n.
/// HEX: 1D 68 n.
/// </summary>
public sealed class BarcodeSetHeightDescriptor : ICommandDescriptor
{
    private const int FixedLength = 3;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1D, 0x68 };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        var height = buffer[2];
        return MatchResult.Matched(new EscPosSetBarcodeHeight(height));
    }
}

/// <summary>
/// ASCII: GS w n.
/// HEX: 1D 77 n.
/// </summary>
public sealed class BarcodeSetModuleWidthDescriptor : ICommandDescriptor
{
    private const int FixedLength = 3;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1D, 0x77 };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        var width = buffer[2];
        return MatchResult.Matched(new EscPosSetBarcodeModuleWidth(width));
    }
}

/// <summary>
/// ASCII: GS H n.
/// HEX: 1D 48 n.
/// </summary>
public sealed class BarcodeSetLabelPositionDescriptor : ICommandDescriptor
{
    private const int FixedLength = 3;
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1D, 0x48 };
    public int MinLength => FixedLength;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        var value = buffer[2];
        if (!TryGetLabelPosition(value, out var position))
        {
            var error = new EscPosParseError("ESCPOS_PARSER_ERROR", $"Invalid barcode label position: 0x{value:X2}. Expected 0x00-0x03");
            return MatchResult.Matched(error);
        }

        return MatchResult.Matched(new EscPosSetBarcodeLabelPosition(position));
    }

    private static bool TryGetLabelPosition(byte value, out EscPosBarcodeLabelPosition position)
    {
        if (value <= 3)
        {
            position = (EscPosBarcodeLabelPosition)value;
            return true;
        }

        position = default;
        return false;
    }
}

/// <summary>
/// Function A
/// ASCII: GS k m d1...dk NUL
/// HEX: 1D 6B m d1...dk 00
///
/// Function B
/// ASCII: GS k m n d1...dn
/// HEX: 1D 6B m n d1...dn
/// </summary>
public sealed class BarcodePrintDescriptor : ICommandDescriptor
{
    private static readonly Encoding Latin1Encoding = Encoding.Latin1;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1D, 0x6B };
    public int MinLength => 4;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => null;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        var symbologyByte = buffer[2];
        if (!TryResolveBarcodeSymbology(symbologyByte, out var symbology))
        {
            var error = new EscPosParseError("ESCPOS_PARSER_ERROR", $"Invalid barcode symbology: 0x{symbologyByte:X2}");
            return MatchResult.Matched(error);
        }

        byte[] payload;

        // Function A: symbologyByte <= 0x06 uses null terminator
        // Function B: symbologyByte >= 0x41 uses length indicator
        var useFunctionB = symbologyByte >= 0x41;

        if (useFunctionB)
        {
            if (buffer.Length < 4)
            {
                return MatchResult.NeedMore();
            }

            var length = buffer[3];
            if (buffer.Length < 4 + length)
            {
                return MatchResult.NeedMore();
            }

            payload = buffer.Slice(4, length).ToArray();
        }
        else
        {
            var payloadStart = 3;
            var terminator = FindNullTerminator(buffer, payloadStart);
            if (terminator == -1)
            {
                return MatchResult.NeedMore();
            }

            var payloadLength = terminator - payloadStart;
            payload = payloadLength > 0 ? buffer.Slice(payloadStart, payloadLength).ToArray() : [];
        }

        // Code128 prefixes such as "{B" select the code set and must not become literal barcode data.
        payload = NormalizePayload(symbology, payload);
        var content = GetPayloadEncoding(symbologyByte).GetString(payload);

        return MatchResult.Matched(new EscPosPrintBarcodeUpload(symbology, content));
    }

    private static bool TryResolveBarcodeSymbology(byte value, out EscPosBarcodeSymbology symbology)
    {
        switch (value)
        {
            case 0x00:
            case 0x41:
                symbology = EscPosBarcodeSymbology.UpcA;
                return true;
            case 0x01:
            case 0x42:
                symbology = EscPosBarcodeSymbology.UpcE;
                return true;
            case 0x02:
            case 0x43:
                symbology = EscPosBarcodeSymbology.Ean13;
                return true;
            case 0x03:
            case 0x44:
                symbology = EscPosBarcodeSymbology.Ean8;
                return true;
            case 0x04:
            case 0x45:
                symbology = EscPosBarcodeSymbology.Code39;
                return true;
            case 0x05:
            case 0x46:
                symbology = EscPosBarcodeSymbology.Itf;
                return true;
            case 0x06:
            case 0x47:
                symbology = EscPosBarcodeSymbology.Codabar;
                return true;
            case 0x48:
                symbology = EscPosBarcodeSymbology.Code93;
                return true;
            case 0x49:
            case 0x4F:
            case 0x08:
                symbology = EscPosBarcodeSymbology.Code128;
                return true;
            default:
                symbology = default;
                return false;
        }
    }

    private static Encoding GetPayloadEncoding(byte symbologyByte)
    {
        // Code128 auto accepts full 8-bit payloads, so Latin-1 preserves bytes 0x00-0xFF one-to-one.
        return symbologyByte == 0x4F
            ? Latin1Encoding
            : Encoding.ASCII;
    }

    private static byte[] NormalizePayload(EscPosBarcodeSymbology symbology, byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (symbology != EscPosBarcodeSymbology.Code128 || payload.Length < 2 || payload[0] != (byte)'{')
        {
            return payload;
        }

        return payload[1] switch
        {
            (byte)'A' or (byte)'B' or (byte)'C' => payload[2..],
            _ => payload
        };
    }

    private static int FindNullTerminator(ReadOnlySpan<byte> data, int startIndex)
    {
        for (var i = startIndex; i < data.Length; i++)
        {
            if (data[i] == 0)
            {
                return i;
            }
        }

        return -1;
    }
}
