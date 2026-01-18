namespace Printify.Domain.Printing;

/// <summary>
/// Parser error emitted when incoming bytes cannot be parsed into a known command.
/// </summary>
public sealed record ParseError(string? Code, string? Message) : Command;
