namespace Printify.Web.Contracts.Printers.Requests;

/// <summary>
/// Cursor-based query parameters for listing printers via the web API.
/// </summary>
/// <param name="Limit">Maximum number of printers to return.</param>
/// <param name="BeforeId">Exclusive upper bound for the identifier cursor.</param>
public sealed record GetPrintersRequestDto(
    int Limit,
    long? BeforeId);
