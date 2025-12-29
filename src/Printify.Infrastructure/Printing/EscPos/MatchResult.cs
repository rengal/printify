using Printify.Domain.Documents.Elements;

namespace Printify.Infrastructure.Printing.EscPos;

public enum MatchKind
{
    Matched,
    NeedMore
}

public sealed class MatchResult
{
    public MatchKind Kind { get; }
    public Element? Element { get; }
    private MatchResult(MatchKind k, Element? el = null)
        => (Kind, Element) = (k, el);

    public static MatchResult NeedMore() => new(MatchKind.NeedMore);
    public static MatchResult Matched(Element? element) => new(MatchKind.Matched, element);
}
