using Printify.Domain.Documents.Elements;

namespace Printify.Infrastructure.Printing.EscPos;

public enum MatchKind { NoMatch, NeedMore, Matched }

public sealed class MatchResult
{
    public MatchKind Kind { get; }
    public int BytesConsumed { get; }
    public Element? Element { get; }
    private MatchResult(MatchKind k, int consumed = 0, Element? el = null)
        => (Kind, BytesConsumed, Element) = (k, consumed, el);

    public static MatchResult NoMatch() => new(MatchKind.NoMatch);
    public static MatchResult NeedMore() => new(MatchKind.NeedMore);
    public static MatchResult Matched(int consumed, Element? el) => new(MatchKind.Matched, consumed, el);
}
