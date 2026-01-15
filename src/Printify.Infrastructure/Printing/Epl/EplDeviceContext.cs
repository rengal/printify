using System.Text;
using Printify.Infrastructure.Printing.Common;

namespace Printify.Infrastructure.Printing.Epl;

/// <summary>
/// Device context for EPL (Eltron Programming Language) protocol.
/// Contains label-specific settings that EPL commands can modify.
/// </summary>
public sealed class EplDeviceContext : IDeviceContext
{
    public Encoding Encoding { get; set; } = Encoding.GetEncoding(437);

    /// <summary>
    /// Label width in dots (set by q command).
    /// </summary>
    public int LabelWidth { get; set; } = 500;

    /// <summary>
    /// Label height in dots (set by Q command).
    /// </summary>
    public int LabelHeight { get; set; } = 300;

    /// <summary>
    /// Print speed (set by R command).
    /// </summary>
    public int PrintSpeed { get; set; } = 2;

    /// <summary>
    /// Print darkness (set by S command).
    /// </summary>
    public int PrintDarkness { get; set; } = 10;
}
