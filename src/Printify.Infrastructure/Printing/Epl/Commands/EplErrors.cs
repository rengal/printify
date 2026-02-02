namespace Printify.Infrastructure.Printing.Epl.Commands;

/// <summary>
/// Parser error emitted when incoming bytes cannot be parsed into a known EPL command.
/// </summary>
public sealed record EplParseError(string? Code, string? Message) : EplCommand;

/// <summary>
/// Printer-side error emitted by the EPL parser or device.
/// </summary>
public sealed record EplPrinterError(string? Message) : EplCommand;
