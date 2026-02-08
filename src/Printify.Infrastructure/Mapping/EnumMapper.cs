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
    public static PagecutMode ParsePagecutMode(string mode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mode);
        return Enum.Parse<PagecutMode>(mode, false);
    }

    public static string ToString(PagecutMode mode)
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

    public static BarcodeSymbology ParseBarcodeSymbology(string symbology)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbology);
        return Enum.Parse<BarcodeSymbology>(symbology, false);
    }

    public static string ToString(BarcodeSymbology symbology)
    {
        return symbology.ToString();
    }

    public static BarcodeLabelPosition ParseBarcodeLabelPosition(string position)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(position);
        return Enum.Parse<BarcodeLabelPosition>(position, false);
    }

    public static string ToString(BarcodeLabelPosition position)
    {
        return position.ToString();
    }

    public static QrErrorCorrectionLevel ParseQrErrorCorrectionLevel(string level)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(level);
        return Enum.Parse<QrErrorCorrectionLevel>(level, false);
    }

    public static string ToString(QrErrorCorrectionLevel level)
    {
        return level.ToString();
    }

    public static QrModel ParseQrModel(string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        return Enum.Parse<QrModel>(model, false);
    }

    public static string ToString(QrModel model)
    {
        return model.ToString();
    }

    public static TextJustification ParseTextJustification(string justification)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(justification);
        return Enum.Parse<TextJustification>(justification, false);
    }

    public static string ToString(TextJustification justification)
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
