namespace Printify.Web.Contracts.Printers.Requests;

/// <summary>
/// Payload used to unpin a previously pinned printer.
/// </summary>
/// <param name="PrinterId">Identifier of the printer to unpin.</param>
public sealed record UnpinPrinterRequestDto(long PrinterId);
