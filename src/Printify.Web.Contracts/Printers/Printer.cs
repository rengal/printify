namespace Printify.Web.Contracts.Printers;

/// <summary>
/// Physical or virtual printer registered by a user or anonymous session.
/// </summary>
/// <param name="Id">Database-generated identifier.</param>
/// <param name="DisplayName">Friendly name shown in UI.</param>
/// <param name="Protocol">Protocol the printer expects (e.g., escpos).</param>
/// <param name="WidthInDots">Configured print width in dots.</param>
/// <param name="HeightInDots">Optional maximum height in dots when known.</param>
public sealed record Printer(
    long Id,
    string DisplayName,
    string Protocol,
    int WidthInDots,
    int? HeightInDots);
