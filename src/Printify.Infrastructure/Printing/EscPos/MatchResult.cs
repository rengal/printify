using Printify.Domain.Documents.Elements;

namespace Printify.Infrastructure.Printing.EscPos;

public enum MatchKind
{
    Matched,
    NeedMore,
    ErrorNotImplemented, // command not implemented
    ErrorNotRecognized, // command not recognized, length unknown
    ErrorInvalid // command recognized, but format or params are invalid
}

public sealed class MatchResult
{
    public MatchKind Kind { get; }
    public Element? Element { get; }
    public string? Warning { get; }
    private MatchResult(MatchKind k, Element? el = null, string? warning = null)
        => (Kind, Element, Warning) = (k, el, warning);

    public static MatchResult Error(MatchKind kind) => new(kind);
    public static MatchResult NeedMore() => new(MatchKind.NeedMore);
    public static MatchResult Matched(Element? element) => new(MatchKind.Matched, element);
    public static MatchResult MatchedWithWarning(Element? element, string warning)
        => new(MatchKind.Matched, element, warning);
}
