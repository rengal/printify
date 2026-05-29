using Printify.Application.Interfaces;
using Printify.Domain.Printing;
using Printify.Domain.Media;
using Printify.Infrastructure.Printing.Common;
using Printify.Infrastructure.Printing.EscPos.Commands;
using System.Text;

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

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        var pL = buffer[3];
        var pH = buffer[4];
        var parameterLength = pL | (pH << 8);
        var cn = buffer[5];
        var fn = buffer[6];
        var payloadLength = parameterLength - 2;
        var payload = payloadLength > 0 ? buffer.Slice(7, payloadLength) : ReadOnlySpan<byte>.Empty;

        if (cn != 0x31)
        {
            var error = new EscPosParseError("ESCPOS_PARSER_ERROR", $"Invalid QR Code command code: 0x{cn:X2}. Expected 0x31");
            return MatchResult.Matched(error);
        }

        Command? element = fn switch
        {
            0x41 when payload.Length > 0 && TryGetQrModel(payload[0], out var model)
                // GS ( k <Function 0x41> - QR Code: Select the model
                => new EscPosSetQrModel(model),
            0x43 when payload.Length > 0
                // GS ( k <Function 0x43> - QR Code: Set the size of module
                => new EscPosSetQrModuleSize(payload[0]),
            0x45 when payload.Length > 0 && TryGetQrErrorCorrection(payload[0], out var level)
                // GS ( k <Function 0x45> - QR Code: Select the error correction level
                => new EscPosSetQrErrorCorrection(level),
            0x50
                // GS ( k <Function 0x50> - QR Code: Store the data in the symbol storage area
                => new EscPosStoreQrData(payload.Length > 1
                    ? Encoding.ASCII.GetString(payload.Slice(1).ToArray())
                    : string.Empty),
            0x51
                // GS ( k <Function 0x51> - QR Code: Print the symbol data in the symbol storage area
                => new EscPosPrintQrCodeUpload(),
            _ => null
        };

        if (element is not null)
        {
            return MatchResult.Matched(element);
        }

        var errorMsg = fn switch
        {
            0x41 when payload.Length == 0 => "QR Code function 0x41 (select model) requires payload",
            0x41 => $"Invalid QR model value: 0x{payload[0]:X2}",
            0x43 when payload.Length == 0 => "QR Code function 0x43 (set module size) requires payload",
            0x45 when payload.Length == 0 => "QR Code function 0x45 (error correction) requires payload",
            0x45 => $"Invalid QR error correction level: 0x{payload[0]:X2}",
            _ => $"Unknown QR Code function: 0x{fn:X2}. Expected 0x41, 0x43, 0x45, 0x50, or 0x51"
        };
        return MatchResult.Matched(new EscPosParseError("ESCPOS_PARSER_ERROR", errorMsg));
    }

    private static bool TryGetQrModel(byte value, out EscPosQrModel model)
    {
        switch (value)
        {
            case 0x31:
            case 0x01:
                model = EscPosQrModel.Model1;
                return true;
            case 0x32:
            case 0x02:
                model = EscPosQrModel.Model2;
                return true;
            case 0x33:
            case 0x03:
                model = EscPosQrModel.Micro;
                return true;
            default:
                model = default;
                return false;
        }
    }

    private static bool TryGetQrErrorCorrection(byte value, out EscPosQrErrorCorrectionLevel level)
    {
        switch (value)
        {
            case (byte)'L':
            case 0x30:
            case 0x00:
                level = EscPosQrErrorCorrectionLevel.Low;
                return true;
            case (byte)'M':
            case 0x31:
            case 0x01:
                level = EscPosQrErrorCorrectionLevel.Medium;
                return true;
            case (byte)'Q':
            case 0x32:
            case 0x02:
                level = EscPosQrErrorCorrectionLevel.Quartile;
                return true;
            case (byte)'H':
            case 0x33:
            case 0x03:
                level = EscPosQrErrorCorrectionLevel.High;
                return true;
            default:
                level = default;
                return false;
        }
    }
}

/// <summary>
/// Command: GS v 0 m xL xH yL yH [data] - raster bit image print.
/// ASCII: GS v 0.
/// HEX: 1D 76 30 m xL xH yL yH ...
/// </summary>
public sealed class RasterBitImagePrintDescriptor(IMediaService mediaService) : ICommandDescriptor
{
    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1D, 0x76, 0x30 };

    // Need at least 8 bytes: GS v 0 m xL xH yL yH
    public int MinLength => 8;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer)
    {
        // Extract width in bytes (little-endian)
        var widthBytes = buffer[4] | (buffer[5] << 8);

        // Extract height in dots (little-endian)
        var height = buffer[6] | (buffer[7] << 8);

        // Calculate total payload length
        var payloadLength = widthBytes * height;

        // Total length: 8 byte header + payload
        return 8 + payloadLength;
    }

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        // Extract parameters
        var widthBytes = buffer[4] | (buffer[5] << 8);
        var heightInDots = buffer[6] | (buffer[7] << 8);
        var payloadLength = widthBytes * heightInDots;

        // Check if we have the complete payload
        if (buffer.Length < 8 + payloadLength)
            return MatchResult.NeedMore();

        // Extract payload data
        var payload = buffer.Slice(8, payloadLength).ToArray();

        // Convert raster data to bitmap
        var widthInDots = widthBytes * 8;
        var bitmap = new MonochromeBitmap(widthInDots, heightInDots, payload);

        // Convert to MediaUpload using IMediaService
        var media = mediaService.ConvertToMediaUpload(bitmap);

        // Create RasterImageContent element
        var element = new EscPosRasterImageUploadGs7630(widthInDots, heightInDots, media);

        // Return matched result with the raster image element
        return MatchResult.Matched(element);
    }
}

/// <summary>
/// Command: GS ( L pL pH ... - graphics data store/print workflow.
/// ASCII: GS ( L.
/// HEX: 1D 28 4C pL pH ...
/// </summary>
public sealed class GraphicsDataShortDescriptor(IMediaService mediaService) : ICommandDescriptor
{
    private const int HeaderLength = 5;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1D, 0x28, 0x4C };

    public int MinLength => HeaderLength;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer)
    {
        var parameterLength = buffer[3] | (buffer[4] << 8);
        return HeaderLength + parameterLength;
    }

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        var parameterLength = buffer[3] | (buffer[4] << 8);
        var payload = buffer.Slice(HeaderLength, parameterLength);
        return GraphicsDataParser.Parse(payload, mediaService, isLongForm: false);
    }
}

/// <summary>
/// Command: GS 8 L p1 p2 p3 p4 ... - long graphics data store workflow.
/// ASCII: GS 8 L.
/// HEX: 1D 38 4C p1 p2 p3 p4 ...
/// </summary>
public sealed class GraphicsDataLongDescriptor(IMediaService mediaService) : ICommandDescriptor
{
    private const int HeaderLength = 7;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1D, 0x38, 0x4C };

    public int MinLength => HeaderLength;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer)
    {
        var parameterLength = buffer[3]
            | (buffer[4] << 8)
            | (buffer[5] << 16)
            | (buffer[6] << 24);

        return HeaderLength + parameterLength;
    }

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        var parameterLength = buffer[3]
            | (buffer[4] << 8)
            | (buffer[5] << 16)
            | (buffer[6] << 24);

        var payload = buffer.Slice(HeaderLength, parameterLength);
        return GraphicsDataParser.Parse(payload, mediaService, isLongForm: true);
    }
}

internal static class GraphicsDataParser
{
    private const int StorePayloadHeaderLength = 10;

    public static MatchResult Parse(ReadOnlySpan<byte> payload, IMediaService mediaService, bool isLongForm)
    {
        if (payload is [0x30, 0x32])
        {
            return MatchResult.Matched(new EscPosRasterImagePrintUploadGs284C());
        }

        if (payload.Length < StorePayloadHeaderLength)
        {
            return MatchResult.Matched(CreateError("GS ( L graphics data command is too short."));
        }

        if (payload[0] != 0x30 || payload[1] != 0x70 || payload[2] != 0x30)
        {
            return MatchResult.Matched(CreateError(
                $"Unsupported GS ( L graphics function: m=0x{payload[0]:X2}, fn=0x{payload[1]:X2}."));
        }

        var widthInDots = payload[6] | (payload[7] << 8);
        var heightInDots = payload[8] | (payload[9] << 8);
        var bytesPerRow = (widthInDots + 7) / 8;
        var imageDataLength = bytesPerRow * heightInDots;
        var imageData = payload[StorePayloadHeaderLength..];

        if (widthInDots <= 0 || heightInDots <= 0 || imageData.Length != imageDataLength)
        {
            return MatchResult.Matched(CreateError(
                "GS ( L graphics data size does not match image dimensions."));
        }

        // GS ( L stores single-color raster data with the same bit order as GS v 0.
        var bitmap = new MonochromeBitmap(widthInDots, heightInDots, imageData.ToArray());
        var media = mediaService.ConvertToMediaUpload(bitmap);
        return isLongForm
            ? MatchResult.Matched(new EscPosRasterImageStoreGs384C(widthInDots, heightInDots, media))
            : MatchResult.Matched(new EscPosRasterImageStoreGs284C(widthInDots, heightInDots, media));
    }

    private static EscPosParseError CreateError(string message)
    {
        return new EscPosParseError("ESCPOS_PARSER_ERROR", message);
    }
}

/// <summary>
/// Command: FS p m n - print stored logo by identifier.
/// ASCII: FS p m n.
/// HEX: 1C 70 m n.
/// </summary>
public sealed class PrintStoredLogoDescriptor : ICommandDescriptor
{
    private const int FixedLength = 4;

    public ReadOnlyMemory<byte> Prefix { get; } = new byte[] { 0x1C, (byte)'p' };
    public int MinLength => FixedLength;

    public int? TryGetExactLength(ReadOnlySpan<byte> buffer) => FixedLength;

    public MatchResult TryParse(ReadOnlySpan<byte> buffer)
    {
        // logoId is the fourth byte (index 3)
        var logoId = buffer[3];
        var element = new EscPosPrintLogo(logoId);
        return MatchResult.Matched(element);
    }
}
