using System.Text;
using Printify.Infrastructure.Printing.Common;

namespace Printify.Infrastructure.Printing.EscPos;

/// <summary>
/// Device context for ESC/POS protocol.
/// ESC/POS is primarily a command-based protocol with minimal persistent state.
/// The encoding can be changed via SetCodePage command.
/// </summary>
public sealed class EscPosDeviceContext : IDeviceContext
{
    /// <summary>
    /// Current encoding for text interpretation. Defaults to OEM-US (DOS) code page 437.
    /// Can be changed via SetCodePage command.
    /// </summary>
    public Encoding Encoding { get; set; } = Encoding.GetEncoding(437);
}
