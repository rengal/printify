namespace Printify.Domain.Printers;

/// <summary>
/// Supported printer protocols for tokenization and rendering.
/// </summary>
public enum Protocol
{
    /// <summary>ESC/POS protocol (Epson-compatible).</summary>
    EscPos = 0,

    /// <summary>EPL protocol (Eltron Programming Language).</summary>
    Epl = 1
}

