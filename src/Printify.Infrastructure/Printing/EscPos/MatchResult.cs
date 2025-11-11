using Printify.Domain.Documents.Elements;

namespace Printify.Infrastructure.Printing.EscPos;

public enum MatchKind { NoMatch, NeedMore, Matched, MatchedPending }

public sealed class MatchResult
{
    public MatchKind Kind { get; }
    public int BytesConsumed { get; }
    public Element? Element { get; }
    public string? Warning { get; }
    private MatchResult(MatchKind k, int consumed = 0, Element? el = null, string? warning = null)
        => (Kind, BytesConsumed, Element, Warning) = (k, consumed, el, warning);

    public static MatchResult NoMatch() => new(MatchKind.NoMatch);
    public static MatchResult NeedMore() => new(MatchKind.NeedMore);
    public static MatchResult Matched(int consumed, Element? element) => new(MatchKind.Matched, consumed, element);
    public static MatchResult MatchedWithWarning(int consumed, Element? element, string warning) 
        => new(MatchKind.Matched, consumed, element, warning);
    public static MatchResult MatchedPending(int consumed, Element? element) 
        => new(MatchKind.MatchedPending, consumed, element);
}
