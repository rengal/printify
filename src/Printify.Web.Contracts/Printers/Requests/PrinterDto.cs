namespace Printify.Web.Contracts.Printers.Requests;

/// <summary>
/// Printer identity payload supplied by clients.
/// </summary>
/// <param name="Id">Client-generated identifier.</param>
/// <param name="DisplayName">Friendly name shown in UI.</param>
public sealed record PrinterDto(
    Guid Id,
    string DisplayName);
