using System.Text;

namespace Printify.Infrastructure.Printing.Common;

/// <summary>
/// Common interface for device-specific context across all printer protocols.
/// Contains shared properties like encoding that all protocols use.
/// </summary>
public interface IDeviceContext
{
    /// <summary>
    /// Current encoding for text interpretation.
    /// </summary>
    Encoding Encoding { get; set; }
}
