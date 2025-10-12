namespace Printify.Web.Contracts.Printers.Requests;

/// <summary>
/// Payload instructing the backend to remove a printer.
/// </summary>
/// <param name="PrinterId">Identifier of the printer to remove.</param>
public sealed record DeletePrinterRequestDto(long PrinterId);
