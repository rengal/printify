namespace Printify.Domain.Printers;

/// <summary>
/// Supported printer protocols for tokenization and rendering.
/// </summary>
public enum Protocol
{
    /// <summary>ESC/POS protocol (Epson-compatible).</summary>
    EscPos = 0,

    /// <summary>EPL protocol (Eltron Programming Language).</summary>
    Epl = 1,

    /// <summary>ZPL protocol (Zebra Programming Language).</summary>
    Zpl = 2,

    /// <summary>TSPL protocol (TSC Printer Language).</summary>
    Tspl = 3,

    /// <summary>SLCS protocol (Sato Barcode Printer Language).</summary>
    Slcs = 4
}

