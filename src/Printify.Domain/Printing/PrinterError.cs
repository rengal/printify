namespace Printify.Domain.Printing;

/// <summary>
/// Printer-side error emitted by the parser or device.
/// </summary>
public sealed record PrinterError(string? Message) : Command;
