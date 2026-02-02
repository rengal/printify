namespace Printify.Infrastructure.Printing.EscPos.Commands;

/// <summary>
/// Parser error emitted when incoming bytes cannot be parsed into a known ESC/POS command.
/// </summary>
public sealed record EscPosParseError(string? Code, string? Message) : EscPosCommand;

/// <summary>
/// Printer-side error emitted by the ESC/POS parser or device.
/// </summary>
public sealed record EscPosPrinterError(string? Message) : EscPosCommand;
