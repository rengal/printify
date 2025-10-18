namespace Printify.Web.Contracts.Printers.Requests;

/// <summary>
/// Payload used to toggle the pinned state of a printer.
/// </summary>
/// <param name="IsPinned">Desired pinned state. Set to <c>true</c> to pin, <c>false</c> to unpin.</param>
public sealed record PinPrinterRequestDto(bool IsPinned);
