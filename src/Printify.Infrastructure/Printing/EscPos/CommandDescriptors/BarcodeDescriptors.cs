using System.Text;
using Printify.Domain.Documents.Elements;

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
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        var height = buffer[2];
        return MatchResult.Matched(FixedLength, new SetBarcodeHeight(height));
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
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        var width = buffer[2];
        return MatchResult.Matched(FixedLength, new SetBarcodeModuleWidth(width));
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
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        var value = buffer[2];
        if (!TryGetLabelPosition(value, out var position))
            return MatchResult.NoMatch();

        return MatchResult.Matched(FixedLength, new SetBarcodeLabelPosition(position));
    }

    private static bool TryGetLabelPosition(byte value, out BarcodeLabelPosition position)
    {
        if (value <= 3)
        {
            position = (BarcodeLabelPosition)value;
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
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1D, 0x6B };
    public int MinLength => 4;
    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => null;
    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        var symbologyByte = buffer[2];
        if (!TryResolveBarcodeSymbology(symbologyByte, out var symbology))
        {
            return MatchResult.NoMatch();
        }

        string content;
        int consumed;

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

            var payload = buffer.Slice(4, length).ToArray();
            content = Encoding.ASCII.GetString(payload);
            consumed = 4 + length;
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
            var payload = payloadLength > 0 ? buffer.Slice(payloadStart, payloadLength).ToArray() : [];
            content = Encoding.ASCII.GetString(payload);
            consumed = terminator + 1;
        }

        return MatchResult.Matched(consumed, new PrintBarcode(symbology, content));
    }

    private static bool TryResolveBarcodeSymbology(byte value, out BarcodeSymbology symbology)
    {
        switch (value)
        {
            case 0x00:
            case 0x41:
                symbology = BarcodeSymbology.UpcA;
                return true;
            case 0x01:
            case 0x42:
                symbology = BarcodeSymbology.UpcE;
                return true;
            case 0x02:
            case 0x43:
                symbology = BarcodeSymbology.Ean13;
                return true;
            case 0x03:
            case 0x44:
                symbology = BarcodeSymbology.Ean8;
                return true;
            case 0x04:
            case 0x45:
                symbology = BarcodeSymbology.Code39;
                return true;
            case 0x05:
            case 0x46:
                symbology = BarcodeSymbology.Itf;
                return true;
            case 0x06:
            case 0x47:
                symbology = BarcodeSymbology.Codabar;
                return true;
            case 0x48:
                symbology = BarcodeSymbology.Code93;
                return true;
            case 0x49:
                symbology = BarcodeSymbology.Code128;
                return true;
            default:
                symbology = default;
                return false;
        }
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
