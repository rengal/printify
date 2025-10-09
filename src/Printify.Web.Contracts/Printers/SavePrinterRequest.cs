namespace Printify.Web.Contracts.Printers;

/// <summary>
/// Represents the payload required to persist printer metadata in the data layer.
/// </summary>
public sealed record SavePrinterRequest(
    string DisplayName,
    string Protocol,
    int WidthInDots,
    int? HeightInDots);
