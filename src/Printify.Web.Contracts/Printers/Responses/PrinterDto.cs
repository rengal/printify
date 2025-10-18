namespace Printify.Web.Contracts.Printers.Responses;

/// <summary>
/// Printable device metadata returned to clients.
/// </summary>
/// <param name="Id">Database-generated identifier.</param>
/// <param name="DisplayName">Friendly name shown in UI.</param>
/// <param name="Protocol">Protocol the printer expects (e.g., escpos).</param>
/// <param name="WidthInDots">Configured print width in dots.</param>
/// <param name="HeightInDots">Optional maximum height in dots when known.</param>
/// <param name="IsPinned">Indicates whether the printer is pinned for quick access.</param>
public sealed record PrinterDto(
    Guid Id,
    string DisplayName,
    string Protocol,
    int WidthInDots,
    int? HeightInDots,
    bool IsPinned);
