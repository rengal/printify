namespace Printify.Web.Contracts.Printers.Requests;

/// <summary>
/// Payload used to mark a printer as pinned for quick access.
/// </summary>
/// <param name="PrinterId">Identifier of the printer to pin.</param>
public sealed record PinPrinterRequest(long PrinterId);
