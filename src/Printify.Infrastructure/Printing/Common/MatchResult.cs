using Printify.Domain.Printing;

namespace Printify.Infrastructure.Printing.Common;

/// <summary>
/// Result kind for command parsing attempts.
/// </summary>
public enum MatchKind
{
    /// <summary>
    /// Command was successfully matched and parsed.
    /// </summary>
    Matched,

    /// <summary>
    /// More data is needed to complete the command.
    /// </summary>
    NeedMore
}

/// <summary>
/// Result of a command parsing attempt.
/// </summary>
public readonly record struct MatchResult
{
    /// <summary>
    /// The match kind (Matched or NeedMore).
    /// </summary>
    public MatchKind Kind { get; }

    /// <summary>
    /// The parsed element (only set when Kind is Matched).
    /// </summary>
    public Command? Element { get; }

    /// <summary>
    /// Number of bytes consumed from the buffer (only set when Kind is Matched).
    /// For protocols where buffer position is managed externally (e.g., ESC/POS), this can be 0.
    /// </summary>
    public int BytesConsumed { get; }

    private MatchResult(MatchKind kind, Command? element, int bytesConsumed)
    {
        Kind = kind;
        Element = element;
        BytesConsumed = bytesConsumed;
    }

    /// <summary>
    /// Creates a successful match result with an element.
    /// The parser should use all available buffer for the command.
    /// </summary>
    public static MatchResult Matched(Command element)
    {
        return new MatchResult(MatchKind.Matched, element, 0);
    }

    /// <summary>
    /// Creates a "need more data" result.
    /// </summary>
    public static MatchResult NeedMore() => new MatchResult(MatchKind.NeedMore, null, 0);

    /// <summary>
    /// Default no-match result.
    /// </summary>
    public static MatchResult NotMatched => default;
}
