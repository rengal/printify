namespace Printify.Domain.Documents.Elements;

/// <summary>
/// A non-printing error event emitted by the tokenizer/session (e.g., buffer overflow).
/// </summary>
/// <param name="Sequence">Monotonic sequence index within the document stream.</param>
/// <param name="Code">Machine-readable code (e.g., "BufferOverflow", "ParseError").</param>
/// <param name="Message">Human-readable description.</param>
public sealed record Error(int Sequence, string Code, string Message) : NonPrintingElement(Sequence);

