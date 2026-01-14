namespace Printify.Domain.Printers;

/// <summary>
/// Protocol string constants used for API communication with the frontend.
/// These values match the lowercase protocol strings sent from the JavaScript client.
/// </summary>
public static class ProtocolConstants
{
    /// <summary>ESC/POS protocol (Epson-compatible) - lowercase API value.</summary>
    public const string EscPos = "escpos";

    /// <summary>EPL protocol (Eltron Programming Language) - lowercase API value.</summary>
    public const string Epl = "epl";

    /// <summary>ZPL protocol (Zebra Programming Language) - lowercase API value.</summary>
    public const string Zpl = "zpl";

    /// <summary>TSPL protocol (TSC Printer Language) - lowercase API value.</summary>
    public const string Tspl = "tspl";

    /// <summary>SLCS protocol (Sato Barcode Printer Language) - lowercase API value.</summary>
    public const string Slcs = "slcs";
}
