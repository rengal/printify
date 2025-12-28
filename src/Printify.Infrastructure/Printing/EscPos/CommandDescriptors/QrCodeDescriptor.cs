using System.Text;
using Printify.Domain.Documents.Elements;

namespace Printify.Infrastructure.Printing.EscPos.CommandDescriptors;

/// <summary>
/// Command: GS ( k - QR configuration and workflow (model, module size, EC level, store data, print).
/// ASCII: GS ( k.
/// HEX: 1D 28 6B pL pH cn fn [data].
/// </summary>
public sealed class QrCodeDescriptor : ICommandDescriptor
{
    private static readonly byte[] PrefixBytes = [0x1D, 0x28, 0x6B];

    public ReadOnlyMemory<byte> Prefix => PrefixBytes;

    public int MinLength => 7; // prefix (3) + pL + pH + cn + fn at minimum

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < MinLength)
        {
            return null;
        }

        var pL = buffer[3];
        var pH = buffer[4];
        var parameterLength = pL | (pH << 8);
        return 5 + parameterLength;
    }

    public MatchResult TryParse(ReadOnlySpan<byte> buffer, ParserState state)
    {
        var pL = buffer[3];
        var pH = buffer[4];
        var parameterLength = pL | (pH << 8);
        var totalLength = 5 + parameterLength;
        var cn = buffer[5];
        var fn = buffer[6];
        var payloadLength = parameterLength - 2;
        var payload = payloadLength > 0 ? buffer.Slice(7, payloadLength) : ReadOnlySpan<byte>.Empty;

        if (cn != 0x31)
            return MatchResult.Error(MatchKind.ErrorInvalid);

        Element? element = fn switch
        {
            0x41 when payload.Length > 0 && TryGetQrModel(payload[0], out var model)
                // GS ( k <Function 0x41> - QR Code: Select the model
                => new SetQrModel(model),
            0x43 when payload.Length > 0
                // GS ( k <Function 0x43> - QR Code: Set the size of module
                => new SetQrModuleSize(payload[0]),
            0x45 when payload.Length > 0 && TryGetQrErrorCorrection(payload[0], out var level)
                // GS ( k <Function 0x45> - QR Code: Select the error correction level
                => new SetQrErrorCorrection(level),
            0x50
                // GS ( k <Function 0x50> - QR Code: Store the data in the symbol storage area
                => new StoreQrData(payload.Length > 1
                    ? Encoding.ASCII.GetString(payload.Slice(1).ToArray())
                    : string.Empty),
            0x51
                // GS ( k <Function 0x51> - QR Code: Print the symbol data in the symbol storage area
                => new PrintQrCodeUpload(),
            _ => null
        };

        return element is not null
            ? MatchResult.Matched(element)
            : MatchResult.Error(MatchKind.ErrorInvalid);
    }

    private static bool TryGetQrModel(byte value, out QrModel model)
    {
        switch (value)
        {
            case 0x31:
            case 0x01:
                model = QrModel.Model1;
                return true;
            case 0x32:
            case 0x02:
                model = QrModel.Model2;
                return true;
            case 0x33:
            case 0x03:
                model = QrModel.Micro;
                return true;
            default:
                model = default;
                return false;
        }
    }

    private static bool TryGetQrErrorCorrection(byte value, out QrErrorCorrectionLevel level)
    {
        switch (value)
        {
            case (byte)'L':
            case 0x30:
            case 0x00:
                level = QrErrorCorrectionLevel.Low;
                return true;
            case (byte)'M':
            case 0x31:
            case 0x01:
                level = QrErrorCorrectionLevel.Medium;
                return true;
            case (byte)'Q':
            case 0x32:
            case 0x02:
                level = QrErrorCorrectionLevel.Quartile;
                return true;
            case (byte)'H':
            case 0x33:
            case 0x03:
                level = QrErrorCorrectionLevel.High;
                return true;
            default:
                level = default;
                return false;
        }
    }
}
