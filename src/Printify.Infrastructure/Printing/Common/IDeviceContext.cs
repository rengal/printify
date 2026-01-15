using System.Text;

namespace Printify.Infrastructure.Printing.Common;

/// <summary>
/// Common interface for device-specific context across all printer protocols.
/// Contains mutable state that changes during parsing, such as the current encoding.
/// </summary>
public interface IDeviceContext
{
    /// <summary>
    /// Current encoding for text interpretation.
    /// This can change during printing (e.g., one line uses Cyrillic, the next uses Arabic).
    /// </summary>
    Encoding Encoding { get; set; }
}
