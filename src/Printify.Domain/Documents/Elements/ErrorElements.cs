namespace Printify.Domain.Documents.Elements;

/// <summary>
/// A non-printing error event emitted by the tokenizer/session (e.g., buffer overflow).
/// </summary>
/// <param name="Code">Machine-readable code (e.g., "BufferOverflow", "ParseError").</param>
/// <param name="Message">Human-readable description.</param>
public sealed record ParseError(string Code, string Message) : NonPrintingElement;

/// <summary>
/// Represents a printer-specific error emitted during tokenization (e.g., simulated buffer overflow).
/// </summary>
/// <param name="Message">Human-readable description of the error.</param>
public sealed record PrinterError(string Message) : NonPrintingElement;
