using Printify.Domain.Printers;
using Printify.Infrastructure.Printing.Epl.Commands;
using Printify.Infrastructure.Printing.EscPos.Commands;

namespace Printify.Infrastructure.Mapping;

/// <summary>
/// Converts between domain enums and their string representations.
/// Used for API communication and database serialization.
/// </summary>
public static class EnumMapper
{
    public static EscPosPagecutMode ParsePagecutMode(string mode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mode);
        return Enum.Parse<EscPosPagecutMode>(mode, false);
    }

    public static string ToString(EscPosPagecutMode mode)
    {
        return mode.ToString();
    }

    public static Protocol ParseProtocol(string protocol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocol);
        return protocol.ToLowerInvariant() switch
        {
            ProtocolConstants.EscPos => Protocol.EscPos,
            ProtocolConstants.Epl => Protocol.Epl,
            ProtocolConstants.Zpl => Protocol.Zpl,
            ProtocolConstants.Tspl => Protocol.Tspl,
            ProtocolConstants.Slcs => Protocol.Slcs,
            _ => throw new ArgumentOutOfRangeException(nameof(protocol), protocol, "Protocol is not supported.")
        };
    }

    public static string ToString(Protocol protocol)
    {
        return protocol switch
        {
            Protocol.EscPos => "EscPos",
            Protocol.Epl => "Epl",
            Protocol.Zpl => "Zpl",
            Protocol.Tspl => "Tspl",
            Protocol.Slcs => "Slcs",
            _ => throw new ArgumentOutOfRangeException(nameof(protocol), protocol, "Unsupported protocol value.")
        };
    }

    public static EscPosBarcodeSymbology ParseBarcodeSymbology(string symbology)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbology);
        return Enum.Parse<EscPosBarcodeSymbology>(symbology, false);
    }

    public static string ToString(EscPosBarcodeSymbology symbology)
    {
        return symbology.ToString();
    }

    public static EscPosBarcodeLabelPosition ParseBarcodeLabelPosition(string position)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(position);
        return Enum.Parse<EscPosBarcodeLabelPosition>(position, false);
    }

    public static string ToString(EscPosBarcodeLabelPosition position)
    {
        return position.ToString();
    }

    public static EscPosQrErrorCorrectionLevel ParseQrErrorCorrectionLevel(string level)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(level);
        return Enum.Parse<EscPosQrErrorCorrectionLevel>(level, false);
    }

    public static string ToString(EscPosQrErrorCorrectionLevel level)
    {
        return level.ToString();
    }

    public static EscPosQrModel ParseQrModel(string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        return Enum.Parse<EscPosQrModel>(model, false);
    }

    public static string ToString(EscPosQrModel model)
    {
        return model.ToString();
    }

    public static EscPosTextJustification ParseTextJustification(string justification)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(justification);
        return Enum.Parse<EscPosTextJustification>(justification, false);
    }

    public static string ToString(EscPosTextJustification justification)
    {
        return justification.ToString();
    }

    public static PrinterTargetState ParsePrinterTargetState(string status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        return Enum.Parse<PrinterTargetState>(status, false);
    }

    public static string ToString(PrinterTargetState status)
    {
        return status.ToString();
    }

    public static PrinterState ParsePrinterState(string status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        return Enum.Parse<PrinterState>(status, false);
    }

    public static string ToString(PrinterState status)
    {
        return status.ToString();
    }

    public static PrinterRealtimeScope ParsePrinterRealtimeScope(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            // Default to state-only to avoid sending full realtime payloads unless explicitly requested.
            return PrinterRealtimeScope.State;
        }

        return scope.Trim().ToLowerInvariant() switch
        {
            "state" => PrinterRealtimeScope.State,
            "full" => PrinterRealtimeScope.Full,
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "Scope must be 'state' or 'full'.")
        };
    }

    public static string ToString(PrinterRealtimeScope scope)
    {
        return scope.ToString();
    }

    public static EplPrintDirection ParsePrintDirection(string direction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(direction);
        return Enum.Parse<EplPrintDirection>(direction, false);
    }

    public static string ToString(EplPrintDirection direction)
    {
        return direction.ToString();
    }
}
